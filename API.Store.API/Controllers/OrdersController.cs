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
    public class OrdersController : ControllerBase
    {
        private readonly APIStoreContext _context;

        public OrdersController(APIStoreContext context)
        {
            _context = context;

        }

        [HttpGet]
        public async Task<IEnumerable<Order>> Get()
        {
            return await _context.Orders.Include(i => i.OrderDetails).ToArrayAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var order = _context.Orders.Include(i => i.OrderDetails).FirstOrDefaultAsync(c => c.Id == id);

            if (order == null) return NotFound();

            return Ok(order);

        }

        [HttpPost]
        public async Task<IActionResult> post(Order order)
        {
            if(order == null) return NotFound("Order not found");

            if(order.OrderDetails == null)
            {
                return BadRequest("Order should have at least one details");
            }

            await _context.Orders.AddAsync(order);
            await _context.OrderDetails.AddRangeAsync(order.OrderDetails);

            await _context.SaveChangesAsync();

            return CreatedAtAction("post", order.Id, order);

        }

        [HttpPut]
        public async Task<IActionResult> put(Order order)
        {
            if (order == null) return NotFound();
            if (order.Id <= 0) return NotFound();

            var existOrders = await _context.Orders.Include(i=>i.OrderDetails).FirstOrDefaultAsync(c=>c.Id == order.Id);
            if (existOrders == null) return NotFound("Not exist ordes in data base");

            existOrders.OrderNumber = order.OrderNumber;
            existOrders.OrderDate = order.OrderDate;
            existOrders.DeliveryDate = order.DeliveryDate;
            existOrders.ClientId = order.ClientId;

            _context.OrderDetails.RemoveRange(existOrders.OrderDetails);

            _context.Orders.Update(existOrders);
            _context.OrderDetails.AddRange(order.OrderDetails);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> delete(Order order)
        {
            if (order == null) return NotFound();

            var existOrders = await _context.Orders.Include(i => i.OrderDetails).FirstOrDefaultAsync(c => c.Id == order.Id);
            if (existOrders == null) return NotFound("Not exist ordes in data base");

            _context.OrderDetails.RemoveRange(existOrders.OrderDetails);
            _context.Orders.Remove(existOrders);
            await _context.SaveChangesAsync();

            return NoContent();
        }


    }
}
