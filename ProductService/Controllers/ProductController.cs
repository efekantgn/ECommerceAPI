using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;
using Microsoft.AspNetCore.Authorization;

namespace ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Tüm işlemler için JWT gereksin
    public class ProductController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public ProductController(ProductDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/product - Tüm ürünleri getir
        [HttpGet]
        [AllowAnonymous] // Yetkilendirme olmadan erişilebilir
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        // ✅ GET: api/product/{id} - Belirli bir ürünü getir
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            return product;
        }

        // ✅ POST: api/product - Yeni ürün oluştur
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            product.Id = Guid.NewGuid(); // GUID ID oluştur
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // ✅ PUT: api/product/{id} - Ürünü güncelle
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, Product updatedProduct)
        {
            if (id != updatedProduct.Id)
                return BadRequest();

            _context.Entry(updatedProduct).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // ✅ DELETE: api/product/{id} - Ürünü sil
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
