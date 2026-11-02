using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WelfareSystem.Api.Models.DTOs.User;
using WelfareSystem.Api.Services.Interfaces;
using WelfareSystem.Api.Services;
using WelfareSystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly UserAdministrationService _administration;
    private readonly AppDbContext _context;

    public UsersController(IUserService userService, UserAdministrationService administration, AppDbContext context)
    {
        _userService = userService;
        _administration = administration;
        _context = context;
    }

    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private async Task<IActionResult> AdminAction(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (UserAdministrationException error) { return StatusCode(error.Status, new { message = error.Message }); }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        { return Conflict(new { message = "The email or employee ID is already in use. Refresh and try again." }); }
    }

    [HttpGet("options"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Options() => Ok(new {
        roles = await _context.Roles.AsNoTracking().OrderBy(r => r.Id).Select(r => new { r.Id, r.Name }).ToListAsync(),
        departments = await _context.Departments.AsNoTracking().OrderBy(d => d.Name).Select(d => new { d.Id, d.Name }).ToListAsync()
    });

    [HttpGet("{id:int}/audit"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Audit(int id) => Ok(await _context.AuditLogs.AsNoTracking()
        .Where(a => a.EntityType == "User" && a.EntityId == id.ToString()).OrderByDescending(a => a.Timestamp).Take(100)
        .Select(a => new { a.Id, a.Action, a.Timestamp, a.Details, ActorName = a.User == null ? "System" : a.User.FirstName + " " + a.User.LastName })
        .ToListAsync());

    public class StatusChangeRequest
    {
        [Required] public bool? IsActive { get; set; }
        [Required, MinLength(3), MaxLength(1000)] public string Reason { get; set; } = string.Empty;
        public DateTime? ExpectedUpdatedAt { get; set; }
    }

    [HttpPatch("{id:int}/status"), Authorize(Policy = "AdminOnly")]
    public Task<IActionResult> SetStatus(int id, [FromBody] StatusChangeRequest request) => AdminAction(async () =>
        Ok(await _administration.SetStatusAsync(id, request.IsActive!.Value, request.Reason, request.ExpectedUpdatedAt, ActorId)));

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var user = await _userService.GetCurrentUserAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "HRAndAbove")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var user = await _userService.UpdateProfileAsync(userId, request);
        if (user == null)
        {
            return BadRequest(new { message = "Update failed. Email or Employee ID may already exist." });
        }

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await AdminAction(async () => {
            var user = await _administration.CreateAsync(request, ActorId);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await AdminAction(async () => Ok(await _administration.UpdateAsync(id, request, ActorId)));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeactivateUser(int id, [FromBody] StatusChangeRequest request)
    {
        return await AdminAction(async () => Ok(await _administration.SetStatusAsync(id, false, request.Reason, request.ExpectedUpdatedAt, ActorId)));
    }
}
