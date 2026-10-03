using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Services;

namespace HardwareStorePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _service;

        public PaymentsController(IPaymentService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreatePaymentDTO dto)
        {
            try
            {
                var created = await _service.AddPaymentAsync(dto);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("bill/{billId}")]
        public async Task<IActionResult> GetByBill(int billId)
        {
            var payments = await _service.GetPaymentsByBillAsync(billId);
            return Ok(payments);
        }
    }
}