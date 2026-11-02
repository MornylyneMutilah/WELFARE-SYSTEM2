namespace WelfareSystem.Api.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(int userId, string email, string role, int tokenVersion = 0);
    string GenerateRefreshToken(int userId, int tokenVersion = 0);
    bool ValidateToken(string token);
}
