using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Auth;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class AuthService : IAuthService
{
    private static readonly HashSet<string> PrivilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "HROfficer", "CommitteeMember", "Management", "Administrator"
    };

    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _context;

    public AuthService(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        AppDbContext context)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            return null;
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        if (!user.IsActive)
        {
            return null;
        }

        var department = await _departmentRepository.GetByIdAsync(user.DepartmentId);

        var token = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.Name);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmployeeId = user.EmployeeId,
                Role = user.Role.Name,
                Department = department!.Name
            }
        };
    }

    public async Task<LoginResponse?> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
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

        // Check if department exists
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId);
        if (department == null)
        {
            return null;
        }

        // Create new user (default to Employee role - RoleId = 1)
        // Auto-set by system: RoleId=1 (Employee), IsActive=true, CreatedAt, UpdatedAt
        // Optional from user: HireDate (defaults to 1 year ago if not provided)
        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmployeeId = request.EmployeeId,
            DepartmentId = request.DepartmentId,
            RoleId = 1, // Employee role (auto-set)
            IsActive = true, // Auto-set
            HireDate = request.HireDate ?? DateTime.UtcNow.AddYears(-1), // Default to 1 year ago if not provided
            CreatedAt = DateTime.UtcNow, // Auto-set
            UpdatedAt = DateTime.UtcNow // Auto-set
        };

        var createdUser = await _userRepository.AddAsync(user);

        if (!string.IsNullOrWhiteSpace(request.RequestedRole) &&
            PrivilegedRoles.Contains(request.RequestedRole.Trim()))
        {
            _context.RoleAccessRequests.Add(new RoleAccessRequest
            {
                UserId = createdUser.Id,
                RequestedRole = request.RequestedRole.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        // Reload user with navigation properties
        var userWithRelations = await _userRepository.GetByIdAsync(createdUser.Id);
        if (userWithRelations == null)
        {
            return null;
        }

        var token = _tokenService.GenerateAccessToken(userWithRelations.Id, userWithRelations.Email, userWithRelations.Role.Name);
        var refreshToken = _tokenService.GenerateRefreshToken(userWithRelations.Id);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = userWithRelations.Id,
                Email = userWithRelations.Email,
                FirstName = userWithRelations.FirstName,
                LastName = userWithRelations.LastName,
                EmployeeId = userWithRelations.EmployeeId,
                Role = userWithRelations.Role.Name,
                Department = userWithRelations.Department.Name
            }
        };
    }

    public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
    {
        if (!_tokenService.ValidateToken(refreshToken))
        {
            return null;
        }

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(refreshToken);
        
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return null;
        }

        if (!int.TryParse(userIdClaim.Value, out int userId))
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        // GetByIdAsync now includes Role and Department
        var department = user.Department;

        var newToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.Name);
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);

        return new LoginResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmployeeId = user.EmployeeId,
                Role = user.Role.Name,
                Department = department!.Name
            }
        };
    }
}
