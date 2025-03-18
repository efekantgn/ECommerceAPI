using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs
{
    public class OrderItemDto
    {
        [Required]
        public Guid ProductId { get; set; } // Ürün ID

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } // Ürün adedi

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; } // Ürünün o anki fiyatı
    }
}
