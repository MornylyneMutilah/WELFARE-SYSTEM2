using WelfareSystem.Api.Models.DTOs.Department;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentService(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<DepartmentResponse?> GetDepartmentByIdAsync(int id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            return null;
        }

        return MapToResponse(department);
    }

    public async Task<IEnumerable<DepartmentResponse>> GetAllDepartmentsAsync()
    {
        var departments = await _departmentRepository.GetAllAsync();
        return departments.Select(MapToResponse);
    }

    public async Task<DepartmentResponse?> CreateDepartmentAsync(CreateDepartmentRequest request)
    {
        // Check if code already exists
        var existingDepartment = await _departmentRepository.GetByCodeAsync(request.Code);
        if (existingDepartment != null)
        {
            return null;
        }

        var department = new Department
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description
        };

        var createdDepartment = await _departmentRepository.AddAsync(department);
        return MapToResponse(createdDepartment);
    }

    public async Task<DepartmentResponse?> UpdateDepartmentAsync(int id, UpdateDepartmentRequest request)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            return null;
        }

        // Check if code is being changed and if it conflicts
        if (department.Code != request.Code)
        {
            var existingDepartment = await _departmentRepository.GetByCodeAsync(request.Code);
            if (existingDepartment != null && existingDepartment.Id != id)
            {
                return null;
            }
        }

        department.Name = request.Name;
        department.Code = request.Code;
        department.Description = request.Description;

        await _departmentRepository.UpdateAsync(department);
        return MapToResponse(department);
    }

    private DepartmentResponse MapToResponse(Department department)
    {
        return new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            Description = department.Description
        };
    }
}
