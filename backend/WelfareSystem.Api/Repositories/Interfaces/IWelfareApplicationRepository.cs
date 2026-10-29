using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IWelfareApplicationRepository : IBaseRepository<WelfareApplication>
{
    Task<IEnumerable<WelfareApplication>> GetByUserIdAsync(int userId);
    Task<IEnumerable<WelfareApplication>> GetByWelfareProgramIdAsync(int welfareProgramId);
    Task<IEnumerable<WelfareApplication>> GetByStatusAsync(int status);
}
