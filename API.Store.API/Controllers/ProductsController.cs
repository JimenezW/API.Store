﻿using API.Store.Data;
using API.Store.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Store.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly APIStoreContext _context;

        public ProductsController(APIStoreContext context)
        {
            _context = context;
            
        }

        [HttpGet]
        public async Task<IEnumerable<Product>> get()
        {
            return await _context.Products.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> get(int id)
        {
            var product = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Id == id);

            if (product == null) { return NotFound(); }

            return Ok(product);
        }

        [HttpGet("GetByCategory/{id}")]
        public async Task<IEnumerable<Product>> GetByCategory(int id)
        {
            return await _context.Products.Where(c=>c.ProductCategoryId == id).ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> post(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("post", product.Id, product);
        }

        [HttpPut]
        public async Task<ActionResult> put(Product product)
        {
            _context.Update(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> delete(Product product)
        {
            if (product == null) { return NotFound(); }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}