namespace HardwareStorePortal.API.Models
{
    public class StockTransaction
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string Type { get; set; } = string.Empty; // Sale / Restock / Adjustment
        public int Quantity { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string? Reference { get; set; } // e.g. "Bill #12"
    }
}