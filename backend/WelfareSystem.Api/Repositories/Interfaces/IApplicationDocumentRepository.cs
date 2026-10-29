using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IApplicationDocumentRepository : IBaseRepository<ApplicationDocument>
{
    Task<IEnumerable<ApplicationDocument>> GetByApplicationIdAsync(int applicationId);
}
