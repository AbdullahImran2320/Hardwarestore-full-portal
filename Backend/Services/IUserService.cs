using HardwareStorePortal.API.DTOs;

namespace HardwareStorePortal.API.Services
{
    public interface IUserService
    {
        Task<List<UserSummaryDTO>> GetAllAsync();
        Task<bool> ResetPasswordAsync(int id, ResetPasswordDTO dto);
        Task<bool> DeleteAsync(int id, int currentUserId);
    }
}
