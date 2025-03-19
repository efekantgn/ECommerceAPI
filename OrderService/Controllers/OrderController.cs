using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly HttpClient _httpClient;

        public OrderController(OrderDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        // ✅ Tüm siparişleri getir
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    OrderDate = o.OrderDate,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status,
                    OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        Price = oi.Price
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        // ✅ Belirli bir siparişi getir
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Id == id)
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    OrderDate = o.OrderDate,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status,
                    OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        Price = oi.Price
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound();

            return Ok(order);
        }

        // ✅ Yeni sipariş oluştur
        [HttpPost]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder(OrderRequestDto orderRequest)
        {
            foreach (var item in orderRequest.OrderItems)
            {
                var stockUpdated = await DeductStockAsync(item.ProductId, item.Quantity);
                if (!stockUpdated)
                    return BadRequest($"Insufficient stock for product {item.ProductId}");
            }
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = orderRequest.UserId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                OrderItems = orderRequest.OrderItems.Select(oi => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };

            order.TotalPrice = order.OrderItems.Sum(item => item.Price * item.Quantity);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var response = new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, response);
        }

        // ✅ Siparişi güncelle
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(Guid id, OrderRequestDto orderUpdate)
        {
            var existingOrder = await _context.Orders.Include(o => o.OrderItems)
                                                      .FirstOrDefaultAsync(o => o.Id == id);
            if (existingOrder == null)
                return NotFound();

            // Öncelikle mevcut OrderItems'ları güncelle
            foreach (var itemUpdate in orderUpdate.OrderItems)
            {
                var existingOrderItem = existingOrder.OrderItems
                                                     .FirstOrDefault(oi => oi.ProductId == itemUpdate.ProductId);

                if (existingOrderItem != null)
                {
                    // Eğer item zaten varsa, quantity ve price'ı güncelle
                    existingOrderItem.Quantity = itemUpdate.Quantity;
                    existingOrderItem.Price = itemUpdate.Price;
                }
                else
                {
                    // Eğer item mevcut değilse, yeni bir OrderItem ekle
                    existingOrder.OrderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = itemUpdate.ProductId,
                        Quantity = itemUpdate.Quantity,
                        Price = itemUpdate.Price
                    });
                }
            }
            existingOrder.Status = orderUpdate.Status;

            // OrderItem'ların toplam fiyatını güncelle
            existingOrder.TotalPrice = existingOrder.OrderItems.Sum(item => item.Price * item.Quantity);
            _context.Orders.Update(existingOrder);

            await _context.SaveChangesAsync();

            return NoContent();
        }


        // ✅ Siparişi sil
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
                return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            order.Status = status;
            order.OrderDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            order.Status = "Cancelled";
            order.OrderDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        [HttpPost("{id}/return")]
        public async Task<IActionResult> ReturnOrder(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            order.Status = "Returned";
            order.OrderDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        public async Task<bool> DeductStockAsync(Guid productId, int quantity)
        {
            var updateStockRequest = new UpdateStockRequest
            {
                ProductId = productId,
                Quantity = quantity
            };

            var request = new HttpRequestMessage(HttpMethod.Put, "http://localhost:5151/product/update-stock")
            {
                Content = JsonContent.Create(updateStockRequest)
            };

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
}
