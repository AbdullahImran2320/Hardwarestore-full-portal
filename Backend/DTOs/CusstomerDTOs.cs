namespace HardwareStorePortal.API.DTOs
{
    public class CustomerDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
    }

    public class CustomerWithBalanceDTO : CustomerDTO
    {
        public decimal TotalOutstanding { get; set; }
        public int BillCount { get; set; }
    }

    public class CreateCustomerDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
    }

    public class UpdateCustomerDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}