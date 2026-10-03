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

            var report = new DailySalesReportDTO
            {
                Date = targetDate,
                TotalSales = bills.Sum(b => b.TotalAmount),
                TotalPaid = bills.Sum(b => b.PaidAmount),
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