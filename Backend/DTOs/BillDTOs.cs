namespace HardwareStorePortal.API.DTOs
{
    // What the client sends to create a bill
    public class CreateBillDTO
    {
        public int? CustomerId { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash / Online
        public List<CreateBillItemDTO> Items { get; set; } = new();
    }

    public class CreateBillItemDTO
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
    }

    // What the server returns
    public class BillDTO
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public List<BillItemDTO> Items { get; set; } = new();
    }

    public class BillItemDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}