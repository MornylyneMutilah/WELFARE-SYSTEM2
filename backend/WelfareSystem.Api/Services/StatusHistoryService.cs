using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class StatusHistoryService : IStatusHistoryService
{
    private readonly IApplicationStatusHistoryRepository _statusHistoryRepository;

    public StatusHistoryService(IApplicationStatusHistoryRepository statusHistoryRepository)
    {
        _statusHistoryRepository = statusHistoryRepository;
    }

    public async Task<IEnumerable<ApplicationStatusHistory>> GetHistoryByApplicationIdAsync(int applicationId)
    {
        return await _statusHistoryRepository.GetByApplicationIdAsync(applicationId);
    }

    public async Task<ApplicationStatusHistory> AddStatusHistoryAsync(int applicationId, int status, int changedBy, string notes)
    {
        var history = new ApplicationStatusHistory
        {
            ApplicationId = applicationId,
            Status = status,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Notes = notes
        };

        var addedHistory = await _statusHistoryRepository.AddAsync(history);
        return addedHistory!;
    }
}
