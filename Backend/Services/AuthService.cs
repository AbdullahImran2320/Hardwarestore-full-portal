using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Admin-only (see AuthController). Does NOT log the new user in.
        public async Task<UserSummaryDTO> RegisterAsync(RegisterDTO dto)
        {
            var username = dto.Username?.Trim() ?? string.Empty;

            if (username.Length < 3 || username.Length > 30)
                throw new InvalidOperationException("Username must be 3 to 30 characters.");

            if (dto.Role != "Admin" && dto.Role != "Staff")
                throw new InvalidOperationException("Role must be Admin or Staff.");

            var passwordError = PasswordRules.Validate(dto.Password);
            if (passwordError != null)
                throw new InvalidOperationException(passwordError);

            var lowered = username.ToLower();
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == lowered))
                throw new InvalidOperationException("Username already taken.");

            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                MustChangePassword = true // the admin knows the first password, so the user picks their own
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserSummaryDTO
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                MustChangePassword = user.MustChangePassword
            };
        }

        public async Task<AuthResponseDTO?> LoginAsync(LoginDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            return new AuthResponseDTO
            {
                Token = GenerateToken(user),
                Username = user.Username,
                Role = user.Role,
                MustChangePassword = user.MustChangePassword
            };
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDTO dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new InvalidOperationException("Current password is incorrect.");

            if (dto.NewPassword != dto.ConfirmPassword)
                throw new InvalidOperationException("New password and confirmation do not match.");

            if (dto.NewPassword == dto.CurrentPassword)
                throw new InvalidOperationException("New password must be different from the current one.");

            var passwordError = PasswordRules.Validate(dto.NewPassword);
            if (passwordError != null)
                throw new InvalidOperationException(passwordError);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();
        }

        private string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("userId", user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8), // one work shift
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
