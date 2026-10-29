namespace WelfareSystem.Api.Services.AnomalyDetection;

public class DocumentAnomalyDetector : IAnomalyDetector
{
    public string DetectorName => "Document Anomaly Detector";
    public string DetectorDescription => "Detects missing or suspicious document patterns";

    public AnomalyDetectionResult Detect(AnomalyDetectionContext context)
    {
        var result = new AnomalyDetectionResult
        {
            DetectorName = DetectorName
        };

        if (context.Documents == null || context.Documents.Count == 0)
        {
            result.HasAnomaly = true;
            result.Severity = AnomalySeverity.Medium;
            result.Message = "No documents uploaded - documentation is typically required";
            result.Details["DocumentCount"] = 0;
            return result;
        }

        // Check if documents are for the correct program
        var requiredDocs = context.Program.RequiredDocuments;
        var requiredDocList = new List<string>();
        
        try
        {
            var docArray = System.Text.Json.JsonSerializer.Deserialize<string[]>(requiredDocs);
            if (docArray != null)
            {
                requiredDocList = docArray.ToList();
            }
        }
        catch
        {
            // If JSON parsing fails, assume no specific requirements
        }

        if (requiredDocList.Count > 0 && context.Documents.Count < requiredDocList.Count)
        {
            result.HasAnomaly = true;
            result.Severity = AnomalySeverity.High;
            result.Message = $"Only {context.Documents.Count} document(s) uploaded out of {requiredDocList.Count} required";
            result.Details["DocumentCount"] = context.Documents.Count;
            result.Details["RequiredDocumentCount"] = requiredDocList.Count;
            result.Details["RequiredDocuments"] = requiredDocList;
        }
        else
        {
            result.HasAnomaly = false;
            result.Message = "Document submission appears appropriate";
            result.Details["DocumentCount"] = context.Documents.Count;
        }

        return result;
    }
}
