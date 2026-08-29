using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> AddAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<List<Product>> GetLowStockAsync();
        Task<Product?> AdjustStockAsync(int id, int quantityChange, string type, string? reference);
    }
}