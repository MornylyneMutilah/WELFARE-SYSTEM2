using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmployeeIdAsync(string employeeId);
    Task<IEnumerable<User>> GetByDepartmentIdAsync(int departmentId);
    Task<IEnumerable<User>> GetByRoleIdAsync(int roleId);
    Task<User?> GetByIdWithIncludesAsync(int id);
}
