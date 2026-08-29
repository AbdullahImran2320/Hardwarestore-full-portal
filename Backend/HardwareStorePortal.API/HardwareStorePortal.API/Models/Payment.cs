namespace HardwareStorePortal.API.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public Bill? Bill { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash / Online
    }
}