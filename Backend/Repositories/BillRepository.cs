using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Repositories
{
    public class BillRepository : IBillRepository
    {
        private readonly AppDbContext _context;

        public BillRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Bill>> GetAllAsync()
        {
            return await _context.Bills
                .Include(b => b.Customer)
                .Include(b => b.BillItems)
                    .ThenInclude(bi => bi.Product)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        public async Task<Bill?> GetByIdAsync(int id)
        {
            return await _context.Bills
                .Include(b => b.Customer)
                .Include(b => b.BillItems)
                    .ThenInclude(bi => bi.Product)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products.FindAsync(productId);
        }

        public async Task<Bill> CreateBillWithItemsAsync(
            Bill bill,
            List<StockTransaction> stockTransactions,
            List<Product> productsToUpdate)
        {
            using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Bills.Add(bill);
                await _context.SaveChangesAsync(); // generates Bill.Id and BillItem rows

                foreach (var st in stockTransactions)
                {
                    st.Reference = $"Bill #{bill.Id}";
                    _context.StockTransactions.Add(st);
                }

                foreach (var product in productsToUpdate)
                {
                    _context.Products.Update(product);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return bill;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Bill>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Bills
                .Include(b => b.BillItems)
                    .ThenInclude(bi => bi.Product)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        public async Task<List<Bill>> GetTodaysBillsAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Bills
                .Include(b => b.BillItems)
                .Where(b => b.BillDate.Date == today)
                .ToListAsync();
        }

        public async Task<bool> DeleteBillAsync(Bill bill, List<StockTransaction> reversalTransactions, List<Product> productsToUpdate)
        {
            using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var st in reversalTransactions)
                {
                    _context.StockTransactions.Add(st);
                }

                foreach (var product in productsToUpdate)
                {
                    _context.Products.Update(product);
                }

                // BillItems and Payments cascade-delete with the Bill (configured in AppDbContext model).
                _context.Bills.Remove(bill);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}