using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; } // Sipariş veren kullanıcı

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public decimal TotalPrice { get; set; } // Toplam fiyat

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public string Status { get; set; } // Yeni alan
    }
}
