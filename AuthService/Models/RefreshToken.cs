namespace AuthService.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }  // Primary key
        public string? Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; } = false;
        public Guid UserId { get; set; }  // Kullanıcı ile ilişkilendirilmiş
    }
}
