using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;

namespace WelfareSystem.Api.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public override async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmployeeIdAsync(string employeeId)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
    }

    public async Task<IEnumerable<User>> GetByDepartmentIdAsync(int departmentId)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Where(u => u.DepartmentId == departmentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetByRoleIdAsync(int roleId)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Where(u => u.RoleId == roleId)
            .ToListAsync();
    }

    public async Task<User?> GetByIdWithIncludesAsync(int id)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Include(u => u.Applications)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
