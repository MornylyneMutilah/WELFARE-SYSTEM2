using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IApplicationStatusHistoryRepository : IBaseRepository<ApplicationStatusHistory>
{
    Task<IEnumerable<ApplicationStatusHistory>> GetByApplicationIdAsync(int applicationId);
}
