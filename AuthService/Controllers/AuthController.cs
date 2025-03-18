using AuthService.Data;
using AuthService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly AuthDbContext _dbContext;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IServiceProvider _serviceProvider;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            AuthDbContext authDbContext,
            RoleManager<IdentityRole<Guid>> roleManager,
            IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dbContext = authDbContext;
            _roleManager = roleManager;
            _serviceProvider = serviceProvider;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Kullanıcıya rol atama
            if (!string.IsNullOrEmpty(model.Role) && await _roleManager.RoleExistsAsync(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);  // Role atama
            }
            else
            {
                return BadRequest(new { message = "Invalid or non-existing role" });
            }
            return Ok(new { message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || !(await _userManager.CheckPasswordAsync(user, model.Password)))
                return Unauthorized(new { message = "Invalid username or password" });

            var jwtToken = GenerateJwtTokenAsync(user);
            var refreshToken = GenerateRefreshToken();

            // Refresh token'ı veritabanına kaydet
            SaveRefreshToken(user.Id, refreshToken);

            return Ok(new { jwtToken, refreshToken });
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            // Kullanıcının rollerini al
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };


            // Kullanıcının rollerini claim olarak ekle
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();  // Refresh token'ı rastgele bir GUID olarak oluşturuyoruz
        }

        private void SaveRefreshToken(Guid userId, string refreshToken)
        {
            using (var scope = _serviceProvider.CreateScope()) // Yeni bir scope oluştur
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

                var token = new RefreshToken
                {
                    Token = refreshToken,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),  // Refresh token geçerlilik süresi (örneğin 7 gün)
                    UserId = userId
                };

                dbContext.RefreshTokens.Add(token);
                dbContext.SaveChanges();
            }
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var refreshTokenRecord = await _dbContext.RefreshTokens.FirstAsync(rt => rt.Token == request.RefreshToken && rt.ExpiryDate > DateTime.UtcNow && !rt.IsRevoked);

            if (refreshTokenRecord == null)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            // Refresh token geçerli, yeni bir access token oluştur
            var user = await _userManager.FindByIdAsync(refreshTokenRecord.UserId.ToString());
            var jwtToken = GenerateJwtTokenAsync(user!);

            // Opsiyonel olarak eski refresh token'ı iptal edebiliriz
            refreshTokenRecord.IsRevoked = true;
            _dbContext.SaveChanges();

            // Yeni refresh token oluştur
            var newRefreshToken = GenerateRefreshToken();
            SaveRefreshToken(user!.Id, newRefreshToken);

            return Ok(new { jwtToken, refreshToken = newRefreshToken });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            // Refresh token'ı veritabanında sorgula
            var refreshTokenRecord = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.ExpiryDate > DateTime.UtcNow && !rt.IsRevoked);

            if (refreshTokenRecord == null)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            // Refresh token'ı iptal et
            refreshTokenRecord.IsRevoked = true;
            await _dbContext.SaveChangesAsync();  // Veritabanına kaydet

            return Ok(new { message = "Logout successful." });
        }

        [Authorize]
        [HttpGet("auth")]
        public IActionResult Authorized()
        {
            return Ok(new { message = "Authorized" });
        }
        [HttpGet("profile")]
        [Authorize]  // Bu endpoint'e yalnızca doğrulanmış kullanıcılar erişebilir.
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.Identity?.Name;  // JWT'deki kullanıcı adını almak
            if (userId == null)
                return Unauthorized(new { message = "User not found" });

            var user = await _userManager.FindByNameAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Profil bilgilerini DTO formatında döndürüyoruz.
            var userProfile = new UserProfileDTO
            {
                Username = user.UserName,
                Email = user.Email,
                Address = user.Address
            };

            return Ok(userProfile);
        }

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(UserProfileDTO request)
        {
            var userId = User.Identity?.Name;  // JWT'deki kullanıcı adını almak
            if (userId == null)
                return Unauthorized(new { message = "User not found" });

            var user = await _userManager.FindByNameAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            user.UserName = request.Username;
            user.Email = request.Email;
            user.Address = request.Address;

            var result = _userManager.UpdateAsync(user);
            if (!result.Result.Succeeded)
            {
                return BadRequest(new { message = "Profile update failed", errors = result.Result.Errors });
            }
            return Ok(new { message = "Profile updated successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public IActionResult AuthorizedAdmin()
        {
            return Ok(new { message = "Authorized" });
        }

    }
}
