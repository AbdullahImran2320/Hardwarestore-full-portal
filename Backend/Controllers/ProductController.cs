using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Services;

namespace HardwareStorePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _service.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _service.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDTO dto)
        {
            var created = await _service.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateProductDTO dto)
        {
            var success = await _service.UpdateProductAsync(id, dto);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteProductAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var products = await _service.GetLowStockProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id}/cost")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCost(int id)
        {
            var cost = await _service.GetProductCostAsync(id);
            if (cost == null) return NotFound();
            return Ok(cost);
        }

        [HttpPut("{id}/cost")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCost(int id, UpdateProductCostDTO dto)
        {
            var success = await _service.UpdateProductCostAsync(id, dto);
            if (!success) return NotFound();
            return NoContent();
        }

        // Manual stock adjustment: restock, correction, damage, etc.
        [HttpPost("{id}/stock-adjustments")]
        public async Task<IActionResult> AdjustStock(int id, AdjustStockDTO dto)
        {
            try
            {
                var product = await _service.AdjustStockAsync(id, dto);
                if (product == null) return NotFound();
                return Ok(product);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}