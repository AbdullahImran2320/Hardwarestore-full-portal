namespace HardwareStorePortal.API.DTOs
{
    public class CopyBackupDTO
    {
        public string? FolderPath { get; set; }
    }

    public class BackupResultDTO
    {
        public string Path { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BackupFileDTO
    {
        public string Name { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BackupListDTO
    {
        public string DefaultFolder { get; set; } = string.Empty;
        public List<BackupFileDTO> Files { get; set; } = new();
    }

    public class RestoreInfoDTO
    {
        public long Users { get; set; }
        public long Products { get; set; }
        public long Customers { get; set; }
        public long Bills { get; set; }
        public string? NewestBillDate { get; set; }
        public string? LastMigration { get; set; }
    }
}
