using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs
{
    public class OrderRequestDto
    {
        [Required]
        public Guid UserId { get; set; } // Siparişi veren kullanıcı ID

        [Required]
        public required List<OrderItemDto> OrderItems { get; set; } // Sipariş ürünleri
    }
}
