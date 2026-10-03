using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Services;

namespace HardwareStorePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillsController : ControllerBase
    {
        private readonly IBillService _service;

        public BillsController(IBillService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bills = await _service.GetAllBillsAsync();
            return Ok(bills);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var bill = await _service.GetBillByIdAsync(id);
            if (bill == null) return NotFound();
            return Ok(bill);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBillDTO dto)
        {
            try
            {
                var created = await _service.CreateBillAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var bills = await _service.GetBillsByCustomerAsync(customerId);
            return Ok(bills);
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetTodaysBills()
        {
            var bills = await _service.GetTodaysBillsAsync();
            return Ok(bills);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteBillAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}