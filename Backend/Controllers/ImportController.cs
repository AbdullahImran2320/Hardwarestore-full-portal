using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Services;

namespace HardwareStorePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ImportController : ControllerBase
    {
        private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const long MaxFileBytes = 5 * 1024 * 1024;

        private readonly ExcelImportService _import;
        private readonly AppDbContext _context;

        public ImportController(ExcelImportService import, AppDbContext context)
        {
            _import = import;
            _context = context;
        }

        [HttpGet("products/template")]
        public async Task<IActionResult> ProductTemplate()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
            return File(_import.BuildProductTemplate(categories), XlsxContentType, "products-import-template.xlsx");
        }

        [HttpGet("customers/template")]
        public IActionResult CustomerTemplate()
        {
            return File(_import.BuildCustomerTemplate(), XlsxContentType, "customers-import-template.xlsx");
        }

        [HttpPost("products/preview")]
        public async Task<IActionResult> PreviewProducts([FromForm] UploadFileForm form)
        {
            return await Run(form.File, stream => _import.PreviewProductsAsync(stream));
        }

        [HttpPost("products/commit")]
        public async Task<IActionResult> CommitProducts([FromForm] ImportCommitForm form)
        {
            return await Run(form.File, stream => _import.CommitProductsAsync(stream, form.SkipInvalid));
        }

        [HttpPost("customers/preview")]
        public async Task<IActionResult> PreviewCustomers([FromForm] UploadFileForm form)
        {
            return await Run(form.File, stream => _import.PreviewCustomersAsync(stream));
        }

        [HttpPost("customers/commit")]
        public async Task<IActionResult> CommitCustomers([FromForm] ImportCommitForm form)
        {
            return await Run(form.File, stream => _import.CommitCustomersAsync(stream, form.SkipInvalid));
        }

        private async Task<IActionResult> Run<T>(IFormFile? file, Func<Stream, Task<T>> action)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Choose an Excel (.xlsx) file first." });

            if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only .xlsx files are supported." });

            if (file.Length > MaxFileBytes)
                return BadRequest(new { message = "The file is larger than 5 MB." });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;

                var result = await action(ms);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
