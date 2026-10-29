using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;

namespace WelfareSystem.Api.Repositories;

public class WelfareProgramRepository : BaseRepository<WelfareProgram>, IWelfareProgramRepository
{
    public WelfareProgramRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<WelfareProgram>> GetActiveProgramsAsync()
    {
        return await _dbSet.Where(p => p.IsActive).ToListAsync();
    }

    public async Task<WelfareProgram?> GetByIdWithRulesAsync(int id)
    {
        return await _dbSet
            .Include(p => p.EligibilityRules)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
