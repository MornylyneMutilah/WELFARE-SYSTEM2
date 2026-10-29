namespace WelfareSystem.Api.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(int userId, string email, string role);
    string GenerateRefreshToken(int userId);
    bool ValidateToken(string token);
}
