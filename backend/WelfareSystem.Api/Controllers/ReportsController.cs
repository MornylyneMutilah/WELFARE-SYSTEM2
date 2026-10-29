using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WelfareSystem.Api.Models.DTOs.Report;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "HRAndAbove")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("applications-by-status")]
    public async Task<IActionResult> GetApplicationsByStatusReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetApplicationsByStatusReportAsync(startDate, endDate);
        return Ok(report);
    }

    [HttpGet("applications-by-department")]
    public async Task<IActionResult> GetApplicationsByDepartmentReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetApplicationsByDepartmentReportAsync(startDate, endDate);
        return Ok(report);
    }

    [HttpGet("applications-by-program")]
    public async Task<IActionResult> GetApplicationsByProgramReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetApplicationsByProgramReportAsync(startDate, endDate);
        return Ok(report);
    }

    [HttpGet("expenditure")]
    public async Task<IActionResult> GetExpenditureReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetExpenditureReportAsync(startDate, endDate);
        return Ok(report);
    }

    [HttpGet("processing-time")]
    public async Task<IActionResult> GetProcessingTimeReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetProcessingTimeReportAsync(startDate, endDate);
        return Ok(report);
    }

    [HttpGet("flagged-applications")]
    public async Task<IActionResult> GetFlaggedApplicationsReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetFlaggedApplicationsReportAsync(startDate, endDate);
        return Ok(report);
    }
}
