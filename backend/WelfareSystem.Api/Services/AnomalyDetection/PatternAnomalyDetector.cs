namespace WelfareSystem.Api.Services.AnomalyDetection;

public class PatternAnomalyDetector : IAnomalyDetector
{
    public string DetectorName => "Pattern Anomaly Detector";
    public string DetectorDescription => "Detects suspicious patterns in application behavior";

    public AnomalyDetectionResult Detect(AnomalyDetectionContext context)
    {
        var result = new AnomalyDetectionResult
        {
            DetectorName = DetectorName
        };

        if (context.UserApplications == null || context.UserApplications.Count == 0)
        {
            result.HasAnomaly = false;
            result.Message = "Insufficient application history for pattern analysis";
            return result;
        }

        // Check for pattern: applying for same program repeatedly
        var programApplications = context.UserApplications
            .Where(a => a.WelfareProgramId == context.Program.Id)
            .ToList();

        if (programApplications.Count >= 3)
        {
            result.HasAnomaly = true;
            result.Severity = AnomalySeverity.High;
            result.Message = $"User has applied for this program {programApplications.Count} times previously";
            result.Details["ProgramApplicationCount"] = programApplications.Count;
            result.Details["PreviousApplicationIds"] = programApplications.Select(a => a.Id).ToList();
        }
        // Check for pattern: high rejection rate
        else
        {
            var rejectedApplications = context.UserApplications
                .Where(a => a.Status == 9) // Rejected
                .ToList();

            if (rejectedApplications.Count >= 2)
            {
                result.HasAnomaly = true;
                result.Severity = AnomalySeverity.Medium;
                result.Message = $"User has {rejectedApplications.Count} previously rejected application(s)";
                result.Details["RejectedCount"] = rejectedApplications.Count;
                result.Details["TotalApplications"] = context.UserApplications.Count;
            }
            else
            {
                result.HasAnomaly = false;
                result.Message = "No concerning patterns detected";
                result.Details["TotalApplications"] = context.UserApplications.Count;
            }
        }

        return result;
    }
}
