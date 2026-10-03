using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HardwareStorePortal.API.Services;

namespace HardwareStorePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly ExcelExportService _export;

        public ExportController(ExcelExportService export)
        {
            _export = export;
        }

        // Purchase price is only included for Admins, matching the existing cost-price rule.
        [HttpGet("products")]
        public async Task<IActionResult> Products()
        {
            var bytes = await _export.ExportProductsAsync(includeCost: HttpContext.User.IsInRole("Admin"));
            return File(bytes, XlsxContentType, $"products-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }

        [HttpGet("customers")]
        public async Task<IActionResult> Customers()
        {
            var bytes = await _export.ExportCustomersAsync();
            return File(bytes, XlsxContentType, $"customers-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }

        [HttpGet("bills")]
        public async Task<IActionResult> Bills([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var bytes = await _export.ExportBillsAsync(from, to);
            return File(bytes, XlsxContentType, $"bills-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }
    }
}
