using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IDepartmentRepository : IBaseRepository<Department>
{
    Task<Department?> GetByCodeAsync(string code);
}
