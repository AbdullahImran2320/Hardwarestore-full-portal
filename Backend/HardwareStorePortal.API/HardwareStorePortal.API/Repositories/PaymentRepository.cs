using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Bill?> GetBillByIdAsync(int billId)
        {
            return await _context.Bills.FindAsync(billId);
        }

        public async Task<Payment> AddPaymentAsync(Payment payment, Bill bill)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Payments.Add(payment);
                _context.Bills.Update(bill);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return payment;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Payment>> GetByBillIdAsync(int billId)
        {
            return await _context.Payments
                .Where(p => p.BillId == billId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }
    }
}