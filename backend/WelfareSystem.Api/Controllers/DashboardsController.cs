using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WelfareSystem.Api.Models.DTOs.Dashboard;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardsController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardsController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("employee")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<IActionResult> GetEmployeeDashboard()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var stats = await _dashboardService.GetEmployeeDashboardStatsAsync(userId);
        return Ok(stats);
    }

    [HttpGet("hr")]
    [Authorize(Policy = "HROnly")]
    public async Task<IActionResult> GetHRDashboard()
    {
        var stats = await _dashboardService.GetHRDashboardStatsAsync();
        return Ok(stats);
    }

    [HttpGet("committee")]
    [Authorize(Policy = "CommitteeOnly")]
    public async Task<IActionResult> GetCommitteeDashboard()
    {
        var stats = await _dashboardService.GetCommitteeDashboardStatsAsync();
        return Ok(stats);
    }

    [HttpGet("management")]
    [Authorize(Policy = "ManagementOnly")]
    public async Task<IActionResult> GetManagementDashboard()
    {
        var stats = await _dashboardService.GetManagementDashboardStatsAsync();
        return Ok(stats);
    }

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var stats = await _dashboardService.GetAdminDashboardStatsAsync();
        return Ok(stats);
    }
}
