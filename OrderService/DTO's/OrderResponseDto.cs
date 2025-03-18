using System;
using System.Collections.Generic;

namespace OrderService.DTOs
{
    public class OrderResponseDto
    {
        public Guid Id { get; set; } // Sipariş ID
        public Guid UserId { get; set; } // Kullanıcı ID
        public DateTime OrderDate { get; set; } // Sipariş tarihi
        public decimal TotalPrice { get; set; } // Toplam fiyat
        public required List<OrderItemDto> OrderItems { get; set; } // Sipariş edilen ürünler
    }
}
