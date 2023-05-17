using API.Store.Data;
using API.Store.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Store.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCategoriesController : ControllerBase
    {
        private readonly APIStoreContext _context;

        public ProductCategoriesController(APIStoreContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IEnumerable<ProductCategory>> get()
        {
            return await _context.ProductCategories.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> get(int id)
        {
            var client = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Id == id);

            if (client == null) { return NotFound(); }

            return Ok(client);
        }

        [HttpPost]
        public async Task<IActionResult> post(ProductCategory product)
        {
            await _context.ProductCategories.AddAsync(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("post", product.Id, product);
        }

        [HttpPut]
        public async Task<ActionResult> put(ProductCategory product)
        {
            _context.Update(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> delete(ProductCategory product)
        {
            if (product == null) { return NotFound(); }

            _context.ProductCategories.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
