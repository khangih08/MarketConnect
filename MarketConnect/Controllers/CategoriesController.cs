using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketConnect.Data;

namespace MarketConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cat = await _service.GetByIdAsync(id);
            if (cat == null) return NotFound();
            return Ok(cat);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category category)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _service.CreateAsync(category);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (System.ArgumentException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Category categoryData)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updated = await _service.UpdateAsync(id, categoryData);
                if (updated == null) return NotFound(new { message = $"Category with ID {id} not found." });
                return Ok(updated);
            }
            catch (System.ArgumentException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _service.DeleteAsync(id);
                if (!ok) return NotFound(new { message = $"Category with ID {id} not found." });
                return NoContent();
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}