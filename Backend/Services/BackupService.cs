using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.DTOs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HardwareStorePortal.API.Services
{
    // Registered as a singleton. It never uses the request's DbContext for file work.
    public class BackupService
    {
        private const int KeepBackups = 10;
        private const int KeepPreRestore = 5;

        // SQLite files start with this 16-byte header.
        private static readonly byte[] SqliteHeader = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0");

        // Only one backup/restore operation at a time.
        private static readonly SemaphoreSlim Gate = new(1, 1);

        private readonly DatabasePaths _paths;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackupService> _logger;

        public BackupService(DatabasePaths paths, IServiceScopeFactory scopeFactory, ILogger<BackupService> logger)
        {
            _paths = paths;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public string DefaultFolder => _paths.BackupFolder;

        // ============================================================
        // Backup
        // ============================================================

        // Creates a consistent snapshot in a temp file. The caller deletes it (or streams it with DeleteOnClose).
        public async Task<string> CreateTempSnapshotAsync()
        {
            await Gate.WaitAsync();
            try
            {
                var temp = Path.Combine(Path.GetTempPath(), "hsp-backup-" + Guid.NewGuid().ToString("N") + ".db");
                SnapshotTo(temp);
                return temp;
            }
            finally
            {
                Gate.Release();
            }
        }

        public static string NewBackupFileName()
        {
            return "hardwarestore-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".db";
        }

        public async Task<BackupResultDTO> CopyToFolderAsync(string? folderPath)
        {
            var folder = string.IsNullOrWhiteSpace(folderPath) ? _paths.BackupFolder : folderPath.Trim().Trim('"');

            if (!Path.IsPathRooted(folder))
                throw new InvalidOperationException("Enter a full folder path, for example D:\\Backups.");

            await Gate.WaitAsync();
            try
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("The folder could not be found or created: " + folder);
                }

                var target = Path.Combine(folder, NewBackupFileName());
                if (File.Exists(target))
                    target = Path.Combine(folder, "hardwarestore-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".db");

                try
                {
                    SnapshotTo(target);
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Backup to {Folder} failed", folder);
                    throw new InvalidOperationException("Could not write the backup to that folder. Check that it exists and is not read-only.");
                }

                if (string.Equals(Path.GetFullPath(folder).TrimEnd('\\', '/'),
                                  Path.GetFullPath(_paths.BackupFolder).TrimEnd('\\', '/'),
                                  StringComparison.OrdinalIgnoreCase))
                {
                    TrimOldFiles("hardwarestore-backup-*.db", KeepBackups);
                }

                var info = new FileInfo(target);
                return new BackupResultDTO { Path = target, SizeBytes = info.Length, CreatedAt = info.LastWriteTime };
            }
            finally
            {
                Gate.Release();
            }
        }

        public BackupListDTO List()
        {
            var dto = new BackupListDTO { DefaultFolder = _paths.BackupFolder };

            if (!Directory.Exists(_paths.BackupFolder)) return dto;

            dto.Files = new DirectoryInfo(_paths.BackupFolder)
                .GetFiles("*.db")
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => new BackupFileDTO { Name = f.Name, SizeBytes = f.Length, CreatedAt = f.LastWriteTime })
                .ToList();

            return dto;
        }

        // Online backup (safe while the app is running) followed by an integrity check of the copy.
        private void SnapshotTo(string targetPath)
        {
            try
            {
                using (var source = new SqliteConnection("Data Source=" + _paths.DbPath + ";Pooling=False"))
                using (var destination = new SqliteConnection("Data Source=" + targetPath + ";Pooling=False"))
                {
                    source.Open();
                    destination.Open();
                    source.BackupDatabase(destination);
                }

                var result = ScalarText(targetPath, "PRAGMA integrity_check;");
                if (result != "ok")
                    throw new InvalidOperationException("The backup failed its integrity check, so it was discarded.");
            }
            catch
            {
                TryDelete(targetPath);
                throw;
            }
        }

        // ============================================================
        // Restore
        // ============================================================

        // Checks an uploaded file without changing anything. Throws InvalidOperationException with a clear message.
        public RestoreInfoDTO ValidateBackupFile(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var header = new byte[SqliteHeader.Length];
                var read = fs.Read(header, 0, header.Length);
                if (read < header.Length || !header.SequenceEqual(SqliteHeader))
                    throw new InvalidOperationException("This is not a SQLite database file.");
            }

            var cs = "Data Source=" + filePath + ";Mode=ReadOnly;Pooling=False";
            var info = new RestoreInfoDTO();

            try
            {
                using var conn = new SqliteConnection(cs);
                conn.Open();

                var integrity = Scalar(conn, "PRAGMA integrity_check;");
                if (integrity != "ok")
                    throw new InvalidOperationException("The backup file is damaged (integrity check failed).");

                foreach (var table in new[] { "Users", "Products", "Customers", "Bills", "__EFMigrationsHistory" })
                {
                    if (!TableExists(conn, table))
                        throw new InvalidOperationException("This file does not look like a Hardware Store Portal backup (table '" + table + "' is missing).");
                }

                info.Users = Count(conn, "Users");
                info.Products = Count(conn, "Products");
                info.Customers = Count(conn, "Customers");
                info.Bills = Count(conn, "Bills");
                info.NewestBillDate = Scalar(conn, "SELECT MAX(BillDate) FROM \"Bills\";");

                if (info.Users == 0)
                    throw new InvalidOperationException("The backup contains no user accounts, so nobody could log in after restoring it.");

                var known = new HashSet<string>();
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    foreach (var m in db.Database.GetMigrations()) known.Add(m);
                }

                string? last = null;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT MigrationId FROM \"__EFMigrationsHistory\" ORDER BY MigrationId;";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var id = reader.GetString(0);
                        last = id;
                        if (!known.Contains(id))
                            throw new InvalidOperationException("This backup was made by a newer version of the portal. Update the portal first.");
                    }
                }
                info.LastMigration = last;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backup validation failed");
                throw new InvalidOperationException("The file could not be read as a Hardware Store Portal backup.");
            }

            return info;
        }

        // Replaces the live database with the uploaded file. A safety copy of the current database
        // is taken first, and put back automatically if anything goes wrong.
        public async Task<string> RestoreAsync(string uploadedFilePath)
        {
            ValidateBackupFile(uploadedFilePath);

            await Gate.WaitAsync();
            try
            {
                Directory.CreateDirectory(_paths.BackupFolder);
                var safety = Path.Combine(_paths.BackupFolder, "pre-restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".db");
                SnapshotTo(safety);

                var staged = _paths.DbPath + ".restore.tmp";
                File.Copy(uploadedFilePath, staged, true);

                try
                {
                    SqliteConnection.ClearAllPools();

                    TryDelete(_paths.DbPath + "-wal");
                    TryDelete(_paths.DbPath + "-shm");
                    TryDelete(_paths.DbPath + "-journal");

                    File.Move(staged, _paths.DbPath, true);

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        DbInitializer.Initialize(db);
                        if (!db.Users.Any())
                            throw new InvalidOperationException("The restored database has no users.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Restore failed, putting the previous database back");
                    TryDelete(staged);
                    SqliteConnection.ClearAllPools();
                    try
                    {
                        File.Copy(safety, _paths.DbPath, true);
                    }
                    catch (Exception copyEx)
                    {
                        _logger.LogError(copyEx, "Could not put the previous database back");
                        throw new InvalidOperationException("The restore failed. Your previous data is safe in: " + safety);
                    }
                    SqliteConnection.ClearAllPools();
                    throw new InvalidOperationException("The restore failed and your previous data was put back. Nothing was changed.");
                }

                SqliteConnection.ClearAllPools();
                TrimOldFiles("pre-restore-*.db", KeepPreRestore);
                return safety;
            }
            finally
            {
                Gate.Release();
            }
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static string? Scalar(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var value = cmd.ExecuteScalar();
            return value == null || value is DBNull ? null : Convert.ToString(value);
        }

        private static string? ScalarText(string dbPath, string sql)
        {
            using var conn = new SqliteConnection("Data Source=" + dbPath + ";Pooling=False");
            conn.Open();
            return Scalar(conn, sql);
        }

        private static bool TableExists(SqliteConnection conn, string table)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            cmd.Parameters.AddWithValue("$name", table);
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private static long Count(SqliteConnection conn, string table)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM \"" + table + "\";";
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        private void TrimOldFiles(string pattern, int keep)
        {
            try
            {
                if (!Directory.Exists(_paths.BackupFolder)) return;

                var old = new DirectoryInfo(_paths.BackupFolder)
                    .GetFiles(pattern)
                    .OrderByDescending(f => f.LastWriteTime)
                    .Skip(keep);

                foreach (var f in old) TryDelete(f.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not remove old backups");
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // best effort
            }
        }
    }
}
