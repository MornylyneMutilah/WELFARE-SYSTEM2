using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;

namespace WelfareSystem.Api.Repositories;

public class ApplicationDocumentRepository : BaseRepository<ApplicationDocument>, IApplicationDocumentRepository
{
    public ApplicationDocumentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ApplicationDocument>> GetByApplicationIdAsync(int applicationId)
    {
        return await _dbSet
            .Where(d => d.ApplicationId == applicationId)
            .ToListAsync();
    }
}
