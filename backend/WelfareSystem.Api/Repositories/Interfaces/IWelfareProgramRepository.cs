using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IWelfareProgramRepository : IBaseRepository<WelfareProgram>
{
    Task<IEnumerable<WelfareProgram>> GetActiveProgramsAsync();
    Task<WelfareProgram?> GetByIdWithRulesAsync(int id);
}
