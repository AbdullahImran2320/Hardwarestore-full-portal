namespace HardwareStorePortal.API.DTOs
{
    // One row of an Excel import preview. Product imports fill Category..ReorderLevel,
    // customer imports fill Phone and Address. Unused fields stay null.
    public class ImportRowDTO
    {
        public int RowNumber { get; set; }
        public string Action { get; set; } = string.Empty; // Create / Update / Error
        public List<string> Messages { get; set; } = new();

        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public int? Stock { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        public int? ReorderLevel { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class ImportPreviewDTO
    {
        public int TotalRows { get; set; }
        public int ToCreate { get; set; }
        public int ToUpdate { get; set; }
        public int ErrorCount { get; set; }
        public List<string> NewCategories { get; set; } = new();
        public List<ImportRowDTO> Rows { get; set; } = new();
    }

    public class ImportResultDTO
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public int CategoriesCreated { get; set; }
    }
}
