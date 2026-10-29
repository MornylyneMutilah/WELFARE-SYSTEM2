namespace WelfareSystem.Api.Services.AnomalyDetection;

public class AmountAnomalyDetector : IAnomalyDetector
{
    public string DetectorName => "Amount Anomaly Detector";
    public string DetectorDescription => "Detects unusual or suspicious request amounts";

    public AnomalyDetectionResult Detect(AnomalyDetectionContext context)
    {
        var result = new AnomalyDetectionResult
        {
            DetectorName = DetectorName
        };

        var requestedAmount = context.RequestedAmount;
        var maxAmount = context.Program.MaxAmount;

        // Check if requesting maximum amount (potential indicator of taking full amount regardless of need)
        if (requestedAmount >= maxAmount * 0.95m)
        {
            result.HasAnomaly = true;
            result.Severity = AnomalySeverity.Medium;
            result.Message = $"Requesting {requestedAmount:F2} which is 95%+ of the maximum amount ({maxAmount:F2})";
            result.Details["RequestedAmount"] = requestedAmount;
            result.Details["MaxAmount"] = maxAmount;
            result.Details["PercentageOfMax"] = (requestedAmount / maxAmount) * 100;
        }
        // Check for suspiciously round numbers
        else if (IsRoundNumber(requestedAmount))
        {
            result.HasAnomaly = true;
            result.Severity = AnomalySeverity.Low;
            result.Message = $"Requested amount is a round number ({requestedAmount:F2}) which may indicate lack of specific calculation";
            result.Details["RequestedAmount"] = requestedAmount;
            result.Details["IsRoundNumber"] = true;
        }
        else
        {
            result.HasAnomaly = false;
            result.Message = "Requested amount appears reasonable";
            result.Details["RequestedAmount"] = requestedAmount;
        }

        return result;
    }

    private bool IsRoundNumber(decimal amount)
    {
        // Check if amount is a round number (e.g., 1000, 5000, 10000)
        return amount % 1000 == 0 || amount % 500 == 0 || amount % 100 == 0;
    }
}
