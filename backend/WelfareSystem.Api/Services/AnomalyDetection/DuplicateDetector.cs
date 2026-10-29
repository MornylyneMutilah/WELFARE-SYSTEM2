namespace WelfareSystem.Api.Services.AnomalyDetection;

public class DuplicateDetector : IAnomalyDetector
{
    public string DetectorName => "Duplicate Application Detector";
    public string DetectorDescription => "Detects duplicate or very similar applications";

    public AnomalyDetectionResult Detect(AnomalyDetectionContext context)
    {
        var result = new AnomalyDetectionResult
        {
            DetectorName = DetectorName
        };

        if (context.UserApplications == null || context.UserApplications.Count == 0)
        {
            result.HasAnomaly = false;
            result.Message = "No previous applications to compare";
            return result;
        }

        // Check for duplicate applications with same program and similar amount
        var duplicates = context.UserApplications
            .Where(a => a.WelfareProgramId == context.Program.Id &&
                       Math.Abs(a.RequestedAmount - context.RequestedAmount) < 5 &&
                       a.SubmittedAt >= DateTime.UtcNow.AddDays(-7))
            .ToList();

        if (duplicates.Any())
        {
            result.HasAnomaly = true;
            result.Severity = AnomalySeverity.High;
            result.Message = $"Found {duplicates.Count} duplicate application(s) within the last 7 days";
            result.Details["DuplicateCount"] = duplicates.Count;
            result.Details["SimilarApplications"] = duplicates.Select(d => d.Id).ToList();
            result.Details["DaysSinceLast"] = duplicates.Max(d => (DateTime.UtcNow - d.SubmittedAt).Days);
        }
        else
        {
            result.HasAnomaly = false;
            result.Message = "No duplicate applications detected";
        }

        return result;
    }
}
