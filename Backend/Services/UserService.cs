using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.DTOs;

namespace HardwareStorePortal.API.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserSummaryDTO>> GetAllAsync()
        {
            var users = await _context.Users.OrderBy(u => u.Username).ToListAsync();

            return users.Select(u => new UserSummaryDTO
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                MustChangePassword = u.MustChangePassword
            }).ToList();
        }

        // Admin resets someone else's password; that person must choose their own at next login.
        public async Task<bool> ResetPasswordAsync(int id, ResetPasswordDTO dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            var passwordError = PasswordRules.Validate(dto.NewPassword);
            if (passwordError != null)
                throw new InvalidOperationException(passwordError);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.MustChangePassword = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int currentUserId)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            if (user.Id == currentUserId)
                throw new InvalidOperationException("You cannot delete your own account.");

            if (user.Role == "Admin")
            {
                var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
                if (adminCount <= 1)
                    throw new InvalidOperationException("You cannot delete the last Admin account.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
