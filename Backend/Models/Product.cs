using Microsoft.EntityFrameworkCore.Migrations;

namespace HardwareStorePortal.API.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LegacyCategoryText { get; set; }
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public string Unit { get; set; } = string.Empty; // e.g. "Piece", "Meter", "Box"
        public int StockQty { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public int ReorderLevel { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
