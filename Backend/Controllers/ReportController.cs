using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.DTOs;

namespace HardwareStorePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("daily-sales")]
        public async Task<IActionResult> GetDailySales([FromQuery] DateTime? date)
        {
            var targetDate = (date ?? DateTime.UtcNow).Date;

            var bills = await _context.Bills
                .Include(b => b.BillItems)
                    .ThenInclude(bi => bi.Product)
                .Where(b => b.BillDate.Date == targetDate)
                .ToListAsync();

            // TotalPaid should reflect actual payments received today, not the
            // PaidAmount snapshot on the bill (which may include older payments).
            var billIds = bills.Select(b => b.Id).ToList();
            // SQLite cannot add up decimals inside SQL, so load the amounts and add them in memory.
            var paymentAmounts = await _context.Payments
                .Where(p => billIds.Contains(p.BillId) && p.PaymentDate.Date == targetDate)
                .Select(p => p.Amount)
                .ToListAsync();
            var totalPaid = paymentAmounts.Sum();

            var report = new DailySalesReportDTO
            {
                Date = targetDate,
                TotalSales = bills.Sum(b => b.TotalAmount),
                TotalPaid = totalPaid,
                TotalOutstanding = bills.Sum(b => b.TotalAmount - b.PaidAmount),
                BillCount = bills.Count,
                TopProducts = bills
                    .SelectMany(b => b.BillItems)
                    .GroupBy(bi => bi.Product!.Name)
                    .Select(g => new TopProductDTO
                    {
                        ProductName = g.Key,
                        QuantitySold = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.LineTotal)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(5)
                    .ToList()
            };

            return Ok(report);
        }
    }
}