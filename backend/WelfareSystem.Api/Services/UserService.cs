using WelfareSystem.Api.Models.DTOs.User;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponse?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdWithIncludesAsync(id);
        if (user == null)
        {
            return null;
        }

        return MapToResponse(user);
    }

    public async Task<UserResponse?> GetCurrentUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdWithIncludesAsync(userId);
        if (user == null)
        {
            return null;
        }

        return MapToResponse(user);
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToResponse);
    }

    public async Task<UserResponse?> CreateUserAsync(CreateUserRequest request)
    {
        // Check if email already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return null;
        }

        var existingEmployeeId = await _userRepository.GetByEmployeeIdAsync(request.EmployeeId);
        if (existingEmployeeId != null)
        {
            return null;
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmployeeId = request.EmployeeId,
            DepartmentId = request.DepartmentId,
            RoleId = request.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(user);
        return MapToResponse(createdUser);
    }

    public async Task<UserResponse?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        // Check if email is being changed and if it conflicts
        if (user.Email != request.Email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != id)
            {
                return null;
            }
        }

        // Check if employeeId is being changed and if it conflicts
        if (user.EmployeeId != request.EmployeeId)
        {
            var existingEmployeeId = await _userRepository.GetByEmployeeIdAsync(request.EmployeeId);
            if (existingEmployeeId != null && existingEmployeeId.Id != id)
            {
                return null;
            }
        }

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.EmployeeId = request.EmployeeId;
        user.DepartmentId = request.DepartmentId;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        
        // Fetch updated user with navigation properties
        var updatedUser = await _userRepository.GetByIdWithIncludesAsync(id);
        return MapToResponse(updatedUser!);
    }

    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<UserResponse?> UpdateProfileAsync(int id, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        var updatedUser = await _userRepository.GetByIdWithIncludesAsync(id);
        return MapToResponse(updatedUser!);
    }

    private UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmployeeId = user.EmployeeId,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name ?? string.Empty,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
