using HardwareStorePortal.API.DTOs;

namespace HardwareStorePortal.API.Services
{
    public interface IAuthService
    {
        Task<UserSummaryDTO> RegisterAsync(RegisterDTO dto);
        Task<AuthResponseDTO?> LoginAsync(LoginDTO dto);
        Task ChangePasswordAsync(int userId, ChangePasswordDTO dto);
    }
}
