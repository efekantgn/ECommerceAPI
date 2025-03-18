using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Models
{
    public class OrderItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; } // Sipariş ID'si

        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        [Required]
        public Guid ProductId { get; set; } // Ürün ID'si

        [Required]
        public int Quantity { get; set; } // Ürün adedi

        [Required]
        public decimal Price { get; set; } // Ürünün o anki fiyatı
    }
}
