using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services;
using WelfareSystem.Api.Models.DTOs.User;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/role-requests")]
[Authorize(Policy = "AdminOnly")]
public class RoleRequestsController : ControllerBase
{
    private static readonly Dictionary<string, string> RoleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HROfficer"] = "HROfficer",
        ["CommitteeMember"] = "CommitteeMember",
        ["Management"] = "Management",
        ["Administrator"] = "Administrator"
    };

    private readonly AppDbContext _context;
    private readonly UserAdministrationService _administration;

    public RoleRequestsController(AppDbContext context, UserAdministrationService administration)
    {
        _context = context;
        _administration = administration;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _context.RoleAccessRequests
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                UserEmail = r.User.Email,
                UserName = r.User.FirstName + " " + r.User.LastName,
                r.RequestedRole,
                r.Status,
                r.CreatedAt
            })
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("{id}/decision")]
    public async Task<IActionResult> Decide(int id, [FromBody] DecisionRequest request)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542001)");
        var item = await _context.RoleAccessRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
        if (item == null)
        {
            return NotFound(new { message = "Request not found." });
        }

        if (item.Status != "Pending")
        {
            return BadRequest(new { message = "This request has already been reviewed." });
        }

        var reviewer = User.FindFirst(ClaimTypes.NameIdentifier);
        int.TryParse(reviewer?.Value, out var reviewerId);

        if (request.Approve)
        {
            if (!RoleMap.TryGetValue(item.RequestedRole, out var roleName))
            {
                return BadRequest(new { message = "Unknown role." });
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                return BadRequest(new { message = "Role does not exist." });
            }

            try {
                await _administration.UpdateAsync(item.UserId, new UpdateUserRequest {
                    Email = item.User.Email, FirstName = item.User.FirstName, LastName = item.User.LastName,
                    EmployeeId = item.User.EmployeeId, DepartmentId = item.User.DepartmentId, RoleId = role.Id,
                    HireDate = item.User.HireDate, IsActive = item.User.IsActive, Reason = $"Approved role request #{item.Id}"
                }, reviewerId);
            } catch (UserAdministrationException error) { return StatusCode(error.Status, new { message = error.Message }); }
            item.Status = "Approved";
        }
        else
        {
            item.Status = "Rejected";
        }

        item.ReviewedByUserId = reviewerId;
        item.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { message = "Updated." });
    }

    public class DecisionRequest
    {
        public bool Approve { get; set; }
    }
}
