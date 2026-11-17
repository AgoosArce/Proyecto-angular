using CLASE_ORM.Entities.DTO.Auth;

namespace CLASE_ORM.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    }
}
