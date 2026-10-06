using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Repositories
{
    public interface IBillRepository
    {
        Task<List<Bill>> GetAllAsync();
        Task<Bill?> GetByIdAsync(int id);
        Task<Product?> GetProductByIdAsync(int productId);
        Task<Bill> CreateBillWithItemsAsync(Bill bill, List<StockTransaction> stockTransactions, List<Product> productsToUpdate, Payment? initialPayment = null);
        Task<List<Bill>> GetByCustomerIdAsync(int customerId);
        Task<List<Bill>> GetTodaysBillsAsync();
        Task<bool> DeleteBillAsync(Bill bill, List<StockTransaction> reversalTransactions, List<Product> productsToUpdate);
    }
}