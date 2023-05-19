using API.Store.Data;
using API.Store.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Store.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly APIStoreContext _context;
        public ClientsController(APIStoreContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IEnumerable<Client>> get()
        {
            return await _context.Clients.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> get(int id)
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c=> c.Id == id);

            if(client == null) { return NotFound(); }

            return Ok(client);
        }

        [HttpPost]
        public async Task<IActionResult> post(Client client)
        {
            await _context.Clients.AddAsync(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction("post",client.Id, client);
        }

        [HttpPut]
        public async Task<ActionResult> put(Client client)
        {
            _context.Update(client);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> delete(Client client)
        {
            if(client == null) { return NotFound(); }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
