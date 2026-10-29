using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WelfareSystem.Api.Models.DTOs.Application;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IWelfareApplicationService _welfareApplicationService;
    private readonly IWorkflowService _workflowService;

    public ApplicationsController(IWelfareApplicationService welfareApplicationService, IWorkflowService workflowService)
    {
        _welfareApplicationService = welfareApplicationService;
        _workflowService = workflowService;
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
        var applications = await _welfareApplicationService.GetAllApplicationsAsync();
        return Ok(applications);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplicationById(int id)
    {
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
        var success = await _welfareApplicationService.CancelApplicationAsync(id);
        if (!success)
        {
            return BadRequest(new { message = "Cancellation failed. Application not found or not in draft status." });
        }

        return Ok(new { message = "Application cancelled successfully" });
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "ManagementAndAbove")]
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
