using WelfareSystem.Api.Models.DTOs.Auth;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> RegisterAsync(RegisterRequest request);
    Task<LoginResponse?> RefreshTokenAsync(string refreshToken);
}
