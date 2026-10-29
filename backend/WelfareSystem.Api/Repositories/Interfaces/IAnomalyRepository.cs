using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IAnomalyRepository : IBaseRepository<Anomaly>
{
    Task<IEnumerable<Anomaly>> GetBySeverityAsync(string severity);
    Task<IEnumerable<Anomaly>> GetByTypeAsync(string anomalyType);
    Task<IEnumerable<Anomaly>> GetByApplicationIdAsync(int applicationId);
    Task<IEnumerable<Anomaly>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Anomaly>> GetByStatusAsync(string status);
    Task<IEnumerable<Anomaly>> GetByDepartmentIdAsync(int departmentId);
    Task<IEnumerable<Anomaly>> GetRecentAsync(int count = 50);
    Task<Dictionary<string, int>> GetSeverityCountsAsync();
    Task<Dictionary<string, int>> GetTypeCountsAsync();
}
