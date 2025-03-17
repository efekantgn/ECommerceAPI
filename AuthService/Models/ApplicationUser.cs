using Microsoft.AspNetCore.Identity;
using System;

namespace AuthService.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? Address { get; set; }  // Kullanıcının adresi
    }
}

