using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            var rows = await _context.SaveChangesAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Product>> GetLowStockAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.StockQty <= p.ReorderLevel)
                .ToListAsync();
        }

        public async Task<Product?> AdjustStockAsync(int id, int quantityChange, string type, string? reference)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return null;

                product.StockQty += quantityChange;

                _context.Products.Update(product);
                _context.StockTransactions.Add(new StockTransaction
                {
                    ProductId = product.Id,
                    Type = type,
                    Quantity = quantityChange,
                    Reference = reference
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return product;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}