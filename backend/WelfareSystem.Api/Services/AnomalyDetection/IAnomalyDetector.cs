namespace WelfareSystem.Api.Services.AnomalyDetection;

public interface IAnomalyDetector
{
    string DetectorName { get; }
    string DetectorDescription { get; }
    AnomalyDetectionResult Detect(AnomalyDetectionContext context);
}
