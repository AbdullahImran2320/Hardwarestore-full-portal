namespace HardwareStorePortal.API.DTOs
{
    // For returning product data
    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int StockQty { get; set; }
        public decimal SalePrice { get; set; }
        public int ReorderLevel { get; set; }
    }

    // For creating a product
    public class CreateProductDTO
    {
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int StockQty { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public int ReorderLevel { get; set; }
    }

    // For updating a product
    public class UpdateProductDTO
    {
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public int ReorderLevel { get; set; }
    }
    // For manually adjusting stock (Restock / Correction / Damage etc.)
    public class AdjustStockDTO
    {
        public int QuantityChange { get; set; } // positive = add stock, negative = remove stock
        public string Type { get; set; } = "Adjustment"; // Restock / Adjustment / Damage / Correction
        public string? Reference { get; set; } // e.g. "Supplier invoice #45"
    }

    public class ProductCostDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
    }

    public class UpdateProductCostDTO
    {
        public decimal PurchasePrice { get; set; }
    }
}