using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Auth;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _context;

    public AuthController(IAuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpGet("registration-departments")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrationDepartments() => Ok(await _context.Departments
        .AsNoTracking().OrderBy(d => d.Name).Select(d => new { d.Id, d.Name }).ToListAsync());

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(request);
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(request);
        if (result == null)
        {
            return BadRequest(new { message = "Registration failed. Email or Employee ID may already exist." });
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // In a production system, you would invalidate the refresh token
        // For now, we'll just return success
        return Ok(new { message = "Logged out successfully" });
    }
}
