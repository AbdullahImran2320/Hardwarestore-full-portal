using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Repositories
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync();
        Task<List<(Customer Customer, decimal TotalOutstanding, int BillCount)>> GetAllWithBalancesAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer> AddAsync(Customer customer);
        Task<bool> UpdateAsync(Customer customer);
        Task<bool> DeleteAsync(int id);
    }
}