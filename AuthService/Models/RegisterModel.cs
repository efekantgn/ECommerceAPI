using System.ComponentModel.DataAnnotations;

namespace AuthService.Models
{
    public class RegisterModel
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(6)]
        public required string Password { get; set; }

        [Required]
        public required string Role { get; set; }  // Kullanıcının rolü
    }
}
