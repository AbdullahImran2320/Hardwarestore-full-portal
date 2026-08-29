using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Models;
using HardwareStorePortal.API.Repositories;

namespace HardwareStorePortal.API.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ProductDTO>> GetAllProductsAsync()
        {
            var products = await _repository.GetAllAsync();
            return products.Select(MapToDTO).ToList();
        }

        public async Task<ProductDTO?> GetProductByIdAsync(int id)
        {
            var product = await _repository.GetByIdAsync(id);
            return product == null ? null : MapToDTO(product);
        }

        public async Task<ProductDTO> CreateProductAsync(CreateProductDTO dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                CategoryId = dto.CategoryId,
                Unit = dto.Unit,
                StockQty = dto.StockQty,
                PurchasePrice = dto.PurchasePrice,
                SalePrice = dto.SalePrice,
                ReorderLevel = dto.ReorderLevel
            };

            var created = await _repository.AddAsync(product);
            return MapToDTO(created);
        }

        public async Task<bool> UpdateProductAsync(int id, UpdateProductDTO dto)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return false;

            product.Name = dto.Name;
            product.CategoryId = dto.CategoryId;
            product.Unit = dto.Unit;
            product.PurchasePrice = dto.PurchasePrice;
            product.SalePrice = dto.SalePrice;
            product.ReorderLevel = dto.ReorderLevel;

            return await _repository.UpdateAsync(product);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<List<ProductDTO>> GetLowStockProductsAsync()
        {
            var products = await _repository.GetLowStockAsync();
            return products.Select(MapToDTO).ToList();
        }

        private static ProductDTO MapToDTO(Product p)
        {
            return new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category?.Name ?? p.LegacyCategoryText ?? string.Empty,
                CategoryId = p.CategoryId,
                Unit = p.Unit,
                StockQty = p.StockQty,
                SalePrice = p.SalePrice,
                ReorderLevel = p.ReorderLevel
            };
        }
        public async Task<ProductCostDTO?> GetProductCostAsync(int id)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return null;

            return new ProductCostDTO
            {
                ProductId = product.Id,
                ProductName = product.Name,
                PurchasePrice = product.PurchasePrice
            };
        }

        public async Task<bool> UpdateProductCostAsync(int id, UpdateProductCostDTO dto)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return false;

            product.PurchasePrice = dto.PurchasePrice;
            return await _repository.UpdateAsync(product);
        }

        public async Task<ProductDTO?> AdjustStockAsync(int id, AdjustStockDTO dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            if (existing.StockQty + dto.QuantityChange < 0)
                throw new InvalidOperationException(
                    $"Cannot reduce stock below zero. Current: {existing.StockQty}, Requested change: {dto.QuantityChange}");

            var updated = await _repository.AdjustStockAsync(id, dto.QuantityChange, dto.Type, dto.Reference);
            return updated == null ? null : MapToDTO(updated);
        }
    }
}