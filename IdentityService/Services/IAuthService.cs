using IdentityService.DTOs;

namespace IdentityService.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<UserResponseDto> RegisterAsync(RegisterDto dto);
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
    }
}