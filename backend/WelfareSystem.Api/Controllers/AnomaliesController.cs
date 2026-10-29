using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnomaliesController : ControllerBase
{
    private readonly IAnomalyDetectionService _anomalyDetectionService;
    private readonly Repositories.Interfaces.IAnomalyRepository _anomalyRepository;

    public AnomaliesController(
        IAnomalyDetectionService anomalyDetectionService,
        Repositories.Interfaces.IAnomalyRepository anomalyRepository)
    {
        _anomalyDetectionService = anomalyDetectionService;
        _anomalyRepository = anomalyRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? severity = null, [FromQuery] string? status = null)
    {
        IEnumerable<Models.Entities.Anomaly> anomalies;

        if (!string.IsNullOrEmpty(severity))
        {
            anomalies = await _anomalyRepository.GetBySeverityAsync(severity);
        }
        else if (!string.IsNullOrEmpty(status))
        {
            anomalies = await _anomalyRepository.GetByStatusAsync(status);
        }
        else
        {
            anomalies = await _anomalyRepository.GetRecentAsync(100);
        }

        return Ok(anomalies.Select(a => new
        {
            a.Id,
            a.AnomalyType,
            a.Severity,
            a.Description,
            a.ApplicationId,
            a.UserId,
            a.Status,
            a.DetectedAt,
            a.ResolvedAt,
            a.ResolvedBy,
            a.ResolutionNotes,
            a.AnomalyScore
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var anomaly = await _anomalyRepository.GetByIdAsync(id);
        if (anomaly == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            anomaly.Id,
            anomaly.AnomalyType,
            anomaly.Severity,
            anomaly.Description,
            anomaly.ApplicationId,
            anomaly.UserId,
            anomaly.Status,
            anomaly.DetectedAt,
            anomaly.ResolvedAt,
            anomaly.ResolvedBy,
            anomaly.ResolutionNotes,
            anomaly.AnomalyScore,
            anomaly.DetectionDetails
        });
    }

    [HttpGet("application/{applicationId}")]
    public async Task<IActionResult> GetByApplicationId(int applicationId)
    {
        var anomalies = await _anomalyRepository.GetByApplicationIdAsync(applicationId);
        return Ok(anomalies.Select(a => new
        {
            a.Id,
            a.AnomalyType,
            a.Severity,
            a.Description,
            a.Status,
            a.DetectedAt
        }));
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var anomalies = await _anomalyRepository.GetByUserIdAsync(userId);
        return Ok(anomalies.Select(a => new
        {
            a.Id,
            a.AnomalyType,
            a.Severity,
            a.Description,
            a.ApplicationId,
            a.Status,
            a.DetectedAt
        }));
    }

    [HttpGet("department/{departmentId}")]
    public async Task<IActionResult> GetByDepartmentId(int departmentId)
    {
        var anomalies = await _anomalyRepository.GetByDepartmentIdAsync(departmentId);
        return Ok(anomalies.Select(a => new
        {
            a.Id,
            a.AnomalyType,
            a.Severity,
            a.Description,
            a.ApplicationId,
            a.UserId,
            a.Status,
            a.DetectedAt
        }));
    }

    [HttpGet("stats/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var severityCounts = await _anomalyRepository.GetSeverityCountsAsync();
        var typeCounts = await _anomalyRepository.GetTypeCountsAsync();

        return Ok(new
        {
            severityCounts,
            typeCounts,
            totalAnomalies = severityCounts.Values.Sum()
        });
    }

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolveRequest request)
    {
        await _anomalyDetectionService.MarkAsResolvedAsync(id, request.ResolvedBy, request.Notes);
        return Ok(new { message = "Anomaly marked as resolved" });
    }

    [HttpPost("{id}/investigate")]
    public async Task<IActionResult> Investigate(int id)
    {
        await _anomalyDetectionService.MarkAsInvestigatingAsync(id);
        return Ok(new { message = "Anomaly marked as investigating" });
    }

    [HttpPost("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(int id, [FromBody] DismissRequest request)
    {
        await _anomalyDetectionService.DismissAnomalyAsync(id, request.ResolvedBy, request.Notes);
        return Ok(new { message = "Anomaly dismissed" });
    }

    [HttpPost("detect/application/{applicationId}")]
    public async Task<IActionResult> DetectForApplication(int applicationId)
    {
        var anomalies = await _anomalyDetectionService.DetectAllAnomaliesForApplicationAsync(applicationId);
        return Ok(anomalies.Select(a => new
        {
            a.Id,
            a.AnomalyType,
            a.Severity,
            a.Description,
            a.Status
        }));
    }

    public class ResolveRequest
    {
        public int ResolvedBy { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class DismissRequest
    {
        public int ResolvedBy { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
