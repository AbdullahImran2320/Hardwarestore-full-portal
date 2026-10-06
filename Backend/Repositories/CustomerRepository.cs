using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task<List<(Customer Customer, decimal TotalOutstanding, int BillCount)>> GetAllWithBalancesAsync()
        {
            // SQLite cannot add up decimals inside SQL, so the balances are worked out in memory.
            var customers = await _context.Customers.ToListAsync();

            var bills = await _context.Bills
                .Where(b => b.CustomerId != null)
                .Select(b => new { b.CustomerId, b.TotalAmount, b.PaidAmount })
                .ToListAsync();

            var totals = bills
                .GroupBy(b => b.CustomerId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => new { Outstanding = g.Sum(x => x.TotalAmount - x.PaidAmount), Count = g.Count() });

            var result = new List<(Customer Customer, decimal TotalOutstanding, int BillCount)>();
            foreach (var c in customers)
            {
                if (totals.TryGetValue(c.Id, out var t))
                    result.Add((c, t.Outstanding, t.Count));
                else
                    result.Add((c, 0m, 0));
            }
            return result;
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task<Customer> AddAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<bool> UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            var rows = await _context.SaveChangesAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Bills)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (customer == null) return false;

            var outstanding = customer.Bills.Sum(b => b.TotalAmount - b.PaidAmount);
            if (outstanding > 0)
                throw new InvalidOperationException(
                    $"Cannot delete customer '{customer.Name}' — they have an outstanding balance of Rs. {outstanding:F0}.");

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}