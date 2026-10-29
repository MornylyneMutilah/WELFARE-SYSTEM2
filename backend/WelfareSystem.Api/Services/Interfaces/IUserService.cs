using WelfareSystem.Api.Models.DTOs.User;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IUserService
{
    Task<UserResponse?> GetUserByIdAsync(int id);
    Task<UserResponse?> GetCurrentUserAsync(int userId);
    Task<IEnumerable<UserResponse>> GetAllUsersAsync();
    Task<UserResponse?> CreateUserAsync(CreateUserRequest request);
    Task<UserResponse?> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<UserResponse?> UpdateProfileAsync(int id, UpdateProfileRequest request);
    Task<bool> DeactivateUserAsync(int id);
}
