namespace HardwareStorePortal.API.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public DateTime BillDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = "Unpaid"; // Paid / Partial / Unpaid
        public string PaymentMethod { get; set; } = "Cash"; // Cash / Online

        public List<BillItem> BillItems { get; set; } = new();
    }
}