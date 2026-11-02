using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IAnomalyDetectionService
{
    Task<Anomaly?> DetectApplicationFrequencyAnomalyAsync(int userId, int? applicationId = null);
    Task<Anomaly?> DetectAmountAnomalyAsync(int applicationId);
    Task<Anomaly?> DetectDuplicateApplicationAsync(int userId, int welfareProgramId, int? applicationId = null);
    Task<Anomaly?> DetectCrossProgramAnomalyAsync(int userId, int? applicationId = null);
    Task<IEnumerable<Anomaly>> DetectAllAnomaliesForApplicationAsync(int applicationId);
    Task MarkAsResolvedAsync(int anomalyId, int resolvedBy, string notes);
    Task MarkAsInvestigatingAsync(int anomalyId);
    Task DismissAnomalyAsync(int anomalyId, int resolvedBy, string notes);
}
