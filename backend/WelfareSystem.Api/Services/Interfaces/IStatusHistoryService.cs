using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IStatusHistoryService
{
    Task<IEnumerable<ApplicationStatusHistory>> GetHistoryByApplicationIdAsync(int applicationId);
    Task<ApplicationStatusHistory> AddStatusHistoryAsync(int applicationId, int status, int changedBy, string notes);
}
