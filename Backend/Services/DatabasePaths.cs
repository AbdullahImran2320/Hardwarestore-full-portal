namespace HardwareStorePortal.API.Services
{
    // Where the SQLite database and its backups live on this PC.
    public class DatabasePaths
    {
        public string DataFolder { get; }
        public string DbPath { get; }
        public string BackupFolder { get; }

        public DatabasePaths(string dataFolder)
        {
            DataFolder = dataFolder;
            DbPath = Path.Combine(dataFolder, "hardwarestore.db");
            BackupFolder = Path.Combine(dataFolder, "Backups");
        }
    }
}
