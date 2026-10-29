using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly AppDbContext _context;
    private readonly IAnomalyRepository _anomalyRepository;
    private readonly IWelfareApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;

    public AnomalyDetectionService(
        AppDbContext context,
        IAnomalyRepository anomalyRepository,
        IWelfareApplicationRepository applicationRepository,
        IUserRepository userRepository)
    {
        _context = context;
        _anomalyRepository = anomalyRepository;
        _applicationRepository = applicationRepository;
        _userRepository = userRepository;
    }

    public async Task<Anomaly?> DetectApplicationFrequencyAnomalyAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        // Get applications from the last 30 days
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentApplications = await _context.WelfareApplications
            .Where(a => a.UserId == userId && a.SubmittedAt >= thirtyDaysAgo)
            .CountAsync();

        // Threshold: More than 3 applications in 30 days is suspicious
        if (recentApplications > 3)
        {
            var anomaly = new Anomaly
            {
                AnomalyType = "ApplicationFrequency",
                Severity = recentApplications > 6 ? "High" : "Medium",
                Description = $"User has submitted {recentApplications} applications in the last 30 days, which exceeds the normal threshold of 3.",
                UserId = userId,
                Status = "Open",
                DetectedAt = DateTime.UtcNow,
                AnomalyScore = Math.Min(1.0, recentApplications / 10.0),
                DetectionDetails = JsonSerializer.Serialize(new
                {
                    applicationCount = recentApplications,
                    timeWindowDays = 30,
                    threshold = 3
                })
            };

            return await _anomalyRepository.AddAsync(anomaly);
        }

        return null;
    }

    public async Task<Anomaly?> DetectAmountAnomalyAsync(int applicationId)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null) return null;

        // Get average amount for this program
        var programAverage = await _context.WelfareApplications
            .Where(a => a.WelfareProgramId == application.WelfareProgramId && a.Status != 0)
            .AverageAsync(a => (double?)a.RequestedAmount) ?? 0;

        // Calculate percentage difference
        var percentageAboveAverage = programAverage > 0
            ? ((double)application.RequestedAmount - programAverage) / programAverage * 100
            : 0;

        // Threshold: More than 50% above average is suspicious
        if (percentageAboveAverage > 50)
        {
            var anomaly = new Anomaly
            {
                AnomalyType = "AmountAnomaly",
                Severity = percentageAboveAverage > 100 ? "Critical" : "High",
                Description = $"Requested amount is {percentageAboveAverage:F1}% above the program average of {programAverage:C}.",
                ApplicationId = applicationId,
                UserId = application.UserId,
                Status = "Open",
                DetectedAt = DateTime.UtcNow,
                AnomalyScore = Math.Min(1.0, percentageAboveAverage / 200.0),
                DetectionDetails = JsonSerializer.Serialize(new
                {
                    requestedAmount = application.RequestedAmount,
                    programAverage = programAverage,
                    percentageAboveAverage = percentageAboveAverage,
                    threshold = 50
                })
            };

            return await _anomalyRepository.AddAsync(anomaly);
        }

        return null;
    }

    public async Task<Anomaly?> DetectDuplicateApplicationAsync(int userId, int welfareProgramId)
    {
        // Check for existing applications for the same program in the last 7 days
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var existingApplication = await _context.WelfareApplications
            .Where(a => a.UserId == userId && a.WelfareProgramId == welfareProgramId && a.SubmittedAt >= sevenDaysAgo)
            .FirstOrDefaultAsync();

        if (existingApplication != null)
        {
            var anomaly = new Anomaly
            {
                AnomalyType = "DuplicateApplication",
                Severity = "Medium",
                Description = $"User has already submitted an application for this program on {existingApplication.SubmittedAt:yyyy-MM-dd}.",
                UserId = userId,
                ApplicationId = existingApplication.Id,
                Status = "Open",
                DetectedAt = DateTime.UtcNow,
                AnomalyScore = 0.7,
                DetectionDetails = JsonSerializer.Serialize(new
                {
                    existingApplicationId = existingApplication.Id,
                    existingSubmissionDate = existingApplication.SubmittedAt,
                    timeWindowDays = 7
                })
            };

            return await _anomalyRepository.AddAsync(anomaly);
        }

        return null;
    }

    public async Task<Anomaly?> DetectCrossProgramAnomalyAsync(int userId)
    {
        // Check if user has applications to multiple similar programs within a short time
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentApplications = await _context.WelfareApplications
            .Include(a => a.WelfareProgram)
            .Where(a => a.UserId == userId && a.SubmittedAt >= thirtyDaysAgo)
            .ToListAsync();

        // Group by program category
        var categoryGroups = recentApplications
            .GroupBy(a => a.WelfareProgram?.Category)
            .Where(g => g.Count() > 1)
            .ToList();

        if (categoryGroups.Any())
        {
            var categories = string.Join(", ", categoryGroups.Select(g => g.Key));
            var anomaly = new Anomaly
            {
                AnomalyType = "CrossProgramAnomaly",
                Severity = "Medium",
                Description = $"User has submitted applications to multiple programs in the same category: {categories}.",
                UserId = userId,
                Status = "Open",
                DetectedAt = DateTime.UtcNow,
                AnomalyScore = 0.6,
                DetectionDetails = JsonSerializer.Serialize(new
                {
                    categories = categoryGroups.Select(g => new { category = g.Key, count = g.Count() }),
                    timeWindowDays = 30
                })
            };

            return await _anomalyRepository.AddAsync(anomaly);
        }

        return null;
    }

    public async Task<IEnumerable<Anomaly>> DetectAllAnomaliesForApplicationAsync(int applicationId)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null) return Enumerable.Empty<Anomaly>();

        var anomalies = new List<Anomaly>();

        // Check for frequency anomaly
        var frequencyAnomaly = await DetectApplicationFrequencyAnomalyAsync(application.UserId);
        if (frequencyAnomaly != null) anomalies.Add(frequencyAnomaly);

        // Check for amount anomaly
        var amountAnomaly = await DetectAmountAnomalyAsync(applicationId);
        if (amountAnomaly != null) anomalies.Add(amountAnomaly);

        // Check for duplicate application
        var duplicateAnomaly = await DetectDuplicateApplicationAsync(application.UserId, application.WelfareProgramId);
        if (duplicateAnomaly != null) anomalies.Add(duplicateAnomaly);

        // Check for cross-program anomaly
        var crossProgramAnomaly = await DetectCrossProgramAnomalyAsync(application.UserId);
        if (crossProgramAnomaly != null) anomalies.Add(crossProgramAnomaly);

        return anomalies;
    }

    public async Task MarkAsResolvedAsync(int anomalyId, int resolvedBy, string notes)
    {
        var anomaly = await _anomalyRepository.GetByIdAsync(anomalyId);
        if (anomaly == null) return;

        anomaly.Status = "Resolved";
        anomaly.ResolvedAt = DateTime.UtcNow;
        anomaly.ResolvedBy = resolvedBy;
        anomaly.ResolutionNotes = notes;

        await _anomalyRepository.UpdateAsync(anomaly);
    }

    public async Task MarkAsInvestigatingAsync(int anomalyId)
    {
        var anomaly = await _anomalyRepository.GetByIdAsync(anomalyId);
        if (anomaly == null) return;

        anomaly.Status = "Investigating";
        await _anomalyRepository.UpdateAsync(anomaly);
    }

    public async Task DismissAnomalyAsync(int anomalyId, int resolvedBy, string notes)
    {
        var anomaly = await _anomalyRepository.GetByIdAsync(anomalyId);
        if (anomaly == null) return;

        anomaly.Status = "Dismissed";
        anomaly.ResolvedAt = DateTime.UtcNow;
        anomaly.ResolvedBy = resolvedBy;
        anomaly.ResolutionNotes = notes;

        await _anomalyRepository.UpdateAsync(anomaly);
    }
}
