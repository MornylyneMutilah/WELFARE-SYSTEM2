using WelfareSystem.Api.Models.DTOs.Department;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IDepartmentService
{
    Task<DepartmentResponse?> GetDepartmentByIdAsync(int id);
    Task<IEnumerable<DepartmentResponse>> GetAllDepartmentsAsync();
    Task<DepartmentResponse?> CreateDepartmentAsync(CreateDepartmentRequest request);
    Task<DepartmentResponse?> UpdateDepartmentAsync(int id, UpdateDepartmentRequest request);
}
