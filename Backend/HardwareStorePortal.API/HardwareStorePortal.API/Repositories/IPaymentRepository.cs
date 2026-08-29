using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Repositories
{
    public interface IPaymentRepository
    {
        Task<Bill?> GetBillByIdAsync(int billId);
        Task<Payment> AddPaymentAsync(Payment payment, Bill bill);
        Task<List<Payment>> GetByBillIdAsync(int billId);
    }
}