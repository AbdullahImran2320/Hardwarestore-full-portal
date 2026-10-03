using HardwareStorePortal.API.DTOs;

namespace HardwareStorePortal.API.Services
{
    public interface IProductService
    {
        Task<List<ProductDTO>> GetAllProductsAsync();
        Task<ProductDTO?> GetProductByIdAsync(int id);
        Task<ProductDTO> CreateProductAsync(CreateProductDTO dto);
        Task<bool> UpdateProductAsync(int id, UpdateProductDTO dto);
        Task<bool> DeleteProductAsync(int id);
        Task<List<ProductDTO>> GetLowStockProductsAsync();
        Task<ProductCostDTO?> GetProductCostAsync(int id);
        Task<bool> UpdateProductCostAsync(int id, UpdateProductCostDTO dto);
        Task<ProductDTO?> AdjustStockAsync(int id, AdjustStockDTO dto);
    }
}