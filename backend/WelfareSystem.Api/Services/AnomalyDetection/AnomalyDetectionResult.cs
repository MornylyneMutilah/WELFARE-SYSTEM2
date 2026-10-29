namespace WelfareSystem.Api.Services.AnomalyDetection;

public class AnomalyDetectionResult
{
    public bool HasAnomaly { get; set; }
    public string DetectorName { get; set; } = string.Empty;
    public AnomalySeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

public class AnomalyDetectionEngineResult
{
    public bool HasAnomalies { get; set; }
    public List<AnomalyDetectionResult> AnomalyResults { get; set; } = new();
    public AnomalySeverity MaxSeverity { get; set; }
    public List<string> WarningMessages { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public enum AnomalySeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
