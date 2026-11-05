using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Services;

public static class ApplicationAccess
{
    public static async Task<IQueryable<WelfareApplication>> Visible(AppDbContext db, int actorId)
    {
        var actor = await db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == actorId && u.IsActive);
        var query = db.WelfareApplications.AsNoTracking();
        if (actor == null) return query.Where(a => false);
        return actor.Role.Name switch {
            "Administrator" or "CommitteeMember" or "Management" => query,
            "HROfficer" => query.Where(a => a.UserId == actorId || a.User.DepartmentId == actor.DepartmentId),
            _ => query.Where(a => a.UserId == actorId)
        };
    }
}
