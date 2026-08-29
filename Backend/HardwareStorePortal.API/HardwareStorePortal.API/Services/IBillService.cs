using HardwareStorePortal.API.DTOs;

namespace HardwareStorePortal.API.Services
{
    public interface IBillService
    {
        Task<List<BillDTO>> GetAllBillsAsync();
        Task<BillDTO?> GetBillByIdAsync(int id);
        Task<BillDTO> CreateBillAsync(CreateBillDTO dto);
        Task<List<BillDTO>> GetBillsByCustomerAsync(int customerId);
        Task<List<BillDTO>> GetTodaysBillsAsync();
        Task<bool> DeleteBillAsync(int id);
    }
}