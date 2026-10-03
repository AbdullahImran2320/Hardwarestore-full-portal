namespace HardwareStorePortal.API.DTOs
{
    public class DailySalesReportDTO
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int BillCount { get; set; }
        public List<TopProductDTO> TopProducts { get; set; } = new();
    }

    public class TopProductDTO
    {
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }
}