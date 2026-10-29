namespace WelfareSystem.Api.Services.AnomalyDetection;

public class FrequencyAbuseDetector : IAnomalyDetector
{
    public string DetectorName => "Frequency Abuse Detector";
    public string DetectorDescription => "Detects excessive application frequency patterns";

    public AnomalyDetectionResult Detect(AnomalyDetectionContext context)
    {
        var result = new AnomalyDetectionResult
        {
            DetectorName = DetectorName
        };

        if (context.UserApplications == null || context.UserApplications.Count == 0)
        {
            result.HasAnomaly = false;
            result.Message = "No previous applications to analyze";
            return result;
        }

        // Check for applications in the last 30 days
        var recentApplications = context.UserApplications
            .Where(a => a.SubmittedAt >= DateTime.UtcNow.AddDays(-30))
            .ToList();

        if (recentApplications.Count >= 5)
        {
            result.HasAnomaly = true;
            result.Severity = AnomalySeverity.Critical;
            result.Message = $"User has submitted {recentApplications.Count} applications in the last 30 days (threshold: 5)";
            result.Details["RecentApplications"] = recentApplications.Count;
            result.Details["Threshold"] = 5;
        }
        else if (recentApplications.Count >= 3)
        {
            result.HasAnomaly = true;
            result.Severity = AnomalySeverity.High;
            result.Message = $"User has submitted {recentApplications.Count} applications in the last 30 days (cautionary threshold: 3)";
            result.Details["RecentApplications"] = recentApplications.Count;
            result.Details["Threshold"] = 3;
        }
        else
        {
            result.HasAnomaly = false;
            result.Message = "Application frequency is within acceptable limits";
            result.Details["RecentApplications"] = recentApplications.Count;
        }

        return result;
    }
}
