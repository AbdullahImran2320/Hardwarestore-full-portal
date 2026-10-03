namespace HardwareStorePortal.API.DTOs
{
    public class CreatePaymentDTO
    {
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash / Online
    }

    public class PaymentDTO
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }
}