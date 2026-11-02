using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WelfareSystem.Api.Models.DTOs.Application;
using WelfareSystem.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IWelfareApplicationService _welfareApplicationService;
    private readonly IWorkflowService _workflowService;
    private readonly AppDbContext _context;

    public ApplicationsController(IWelfareApplicationService welfareApplicationService, IWorkflowService workflowService, AppDbContext context)
    {
        _welfareApplicationService = welfareApplicationService;
        _workflowService = workflowService;
        _context = context;
    }

    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private async Task<bool> OwnsApplication(int id) => await _context.WelfareApplications
        .AnyAsync(a => a.Id == id && a.UserId == ActorId && a.User.IsActive);

    private async Task<bool> CanReadApplication(int id)
    {
        var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == ActorId && u.IsActive);
        if (actor == null) return false;
        return await _context.WelfareApplications.AnyAsync(a => a.Id == id &&
            (a.UserId == actor.Id || actor.Role.Name == "Administrator" || actor.Role.Name == "CommitteeMember" ||
             actor.Role.Name == "Management" || (actor.Role.Name == "HROfficer" && a.User.DepartmentId == actor.DepartmentId)));
    }

    [HttpGet]
    public async Task<IActionResult> GetApplications()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var applications = await _welfareApplicationService.GetApplicationsByUserIdAsync(userId);
        return Ok(applications);
    }

    [HttpGet("all")]
    [Authorize(Policy = "HRAndAbove")]
    public async Task<IActionResult> GetAllApplications()
    {
        var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == ActorId && u.IsActive);
        if (actor == null) return Forbid();
        if (actor.Role.Name is not ("HROfficer" or "CommitteeMember" or "Management" or "Administrator")) return Forbid();
        var applications = await _welfareApplicationService.GetAllApplicationsAsync();
        if (actor.Role.Name == "HROfficer")
        {
            var ids = await _context.Users.Where(u => u.DepartmentId == actor.DepartmentId).Select(u => u.Id).ToListAsync();
            applications = applications.Where(a => ids.Contains(a.UserId));
        }
        return Ok(applications);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplicationById(int id)
    {
        if (!await CanReadApplication(id)) return NotFound();
        var application = await _welfareApplicationService.GetApplicationByIdAsync(id);
        if (application == null)
        {
            return NotFound(new { message = "Application not found" });
        }

        return Ok(application);
    }

    [HttpPost]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request)
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

        var application = await _welfareApplicationService.CreateApplicationAsync(userId, request);
        if (application == null)
        {
            return BadRequest(new { message = "Application creation failed. Welfare program may not exist or is inactive." });
        }

        return CreatedAtAction(nameof(GetApplicationById), new { id = application.Id }, application);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApplication(int id, [FromBody] UpdateApplicationRequest request)
    {
        if (!await OwnsApplication(id)) return NotFound();
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var application = await _welfareApplicationService.UpdateApplicationAsync(id, request);
        if (application == null)
        {
            return BadRequest(new { message = "Update failed. Application not found or not in draft status." });
        }

        return Ok(application);
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitApplication(int id)
    {
        if (!await OwnsApplication(id)) return NotFound();
        var application = await _welfareApplicationService.SubmitApplicationAsync(id);
        if (application == null)
        {
            return BadRequest(new { message = "Submission failed. Application not found or not in draft status." });
        }

        return Ok(application);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelApplication(int id)
    {
        if (!await OwnsApplication(id)) return NotFound();
        var success = await _welfareApplicationService.CancelApplicationAsync(id);
        if (!success)
        {
            return BadRequest(new { message = "Cancellation failed. Application not found or not in draft status." });
        }

        return Ok(new { message = "Application cancelled successfully" });
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "HRAndAbove")]
    public async Task<IActionResult> UpdateApplicationStatus(int id, [FromBody] StatusUpdateRequest request)
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

        var roleClaim = User.FindFirst(ClaimTypes.Role);
        if (roleClaim == null)
        {
            return Unauthorized(new { message = "Invalid user role" });
        }

        var success = await _workflowService.UpdateApplicationStatusAsync(id, request.Status, request.Comments, userId, roleClaim.Value);
        if (!success)
        {
            return BadRequest(new { message = "Status update failed. Invalid transition or application not found." });
        }

        var application = await _welfareApplicationService.GetApplicationByIdAsync(id);
        return Ok(application);
    }

    [HttpGet("{id}/anomalies")]
    public async Task<IActionResult> GetApplicationAnomalies(int id)
    {
        if (!await CanReadApplication(id)) return NotFound();
        var application = await _welfareApplicationService.GetApplicationByIdAsync(id);
        if (application == null)
        {
            return NotFound(new { message = "Application not found" });
        }

        // Parse and return anomaly flags
        try
        {
            var anomalyFlags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(application.AnomalyFlags ?? "[]");
            var ruleEvaluationResult = System.Text.Json.JsonSerializer.Deserialize<object>(application.RuleEvaluationResult ?? "{}");
            
            return Ok(new
            {
                anomalyFlags = anomalyFlags,
                ruleEvaluationResult = ruleEvaluationResult
            });
        }
        catch
        {
            return Ok(new
            {
                anomalyFlags = new List<string>(),
                ruleEvaluationResult = new Dictionary<string, object>()
            });
        }
    }
}
