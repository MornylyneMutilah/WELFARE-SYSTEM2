using WelfareSystem.Api.Models.DTOs.WelfareProgram;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class WelfareProgramService : IWelfareProgramService
{
    private readonly IWelfareProgramRepository _welfareProgramRepository;

    public WelfareProgramService(IWelfareProgramRepository welfareProgramRepository)
    {
        _welfareProgramRepository = welfareProgramRepository;
    }

    public async Task<WelfareProgramResponse?> GetProgramByIdAsync(int id)
    {
        var program = await _welfareProgramRepository.GetByIdAsync(id);
        if (program == null)
        {
            return null;
        }

        return MapToResponse(program);
    }

    public async Task<IEnumerable<WelfareProgramResponse>> GetAllProgramsAsync()
    {
        var programs = await _welfareProgramRepository.GetAllAsync();
        return programs.Select(MapToResponse);
    }

    public async Task<IEnumerable<WelfareProgramResponse>> GetActiveProgramsAsync()
    {
        var programs = await _welfareProgramRepository.GetActiveProgramsAsync();
        return programs.Select(MapToResponse);
    }

    public async Task<WelfareProgramResponse?> CreateProgramAsync(CreateWelfareProgramRequest request)
    {
        if (!DocumentListValidator.IsValid(request.RequiredDocuments) || string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Category) ||
            request.MaxAmount is < 0.01m or > 1000000m || request.MinServicePeriod is < 0 or > 120) return null;
        var program = new WelfareProgram
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            MaxAmount = request.MaxAmount,
            MinServicePeriod = request.MinServicePeriod,
            RequiredDocuments = request.RequiredDocuments,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdProgram = await _welfareProgramRepository.AddAsync(program);
        return MapToResponse(createdProgram);
    }

    public async Task<WelfareProgramResponse?> UpdateProgramAsync(int id, UpdateWelfareProgramRequest request)
    {
        if (!DocumentListValidator.IsValid(request.RequiredDocuments) || string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Category) ||
            request.MaxAmount is < 0.01m or > 1000000m || request.MinServicePeriod is < 0 or > 120) return null;
        var program = await _welfareProgramRepository.GetByIdAsync(id);
        if (program == null)
        {
            return null;
        }

        program.Name = request.Name.Trim();
        program.Description = request.Description.Trim();
        program.Category = request.Category.Trim();
        program.MaxAmount = request.MaxAmount;
        program.MinServicePeriod = request.MinServicePeriod;
        program.RequiredDocuments = request.RequiredDocuments;
        program.IsActive = request.IsActive;
        program.UpdatedAt = DateTime.UtcNow;

        await _welfareProgramRepository.UpdateAsync(program);
        
        var updatedProgram = await _welfareProgramRepository.GetByIdAsync(id);
        return MapToResponse(updatedProgram!);
    }

    public async Task<bool> DeactivateProgramAsync(int id)
    {
        var program = await _welfareProgramRepository.GetByIdAsync(id);
        if (program == null)
        {
            return false;
        }

        program.IsActive = false;
        program.UpdatedAt = DateTime.UtcNow;

        await _welfareProgramRepository.UpdateAsync(program);
        return true;
    }

    private WelfareProgramResponse MapToResponse(WelfareProgram program)
    {
        return new WelfareProgramResponse
        {
            Id = program.Id,
            Name = program.Name,
            Description = program.Description,
            Category = program.Category,
            MaxAmount = program.MaxAmount,
            MinServicePeriod = program.MinServicePeriod,
            RequiredDocuments = program.RequiredDocuments,
            IsActive = program.IsActive,
            CreatedAt = program.CreatedAt,
            UpdatedAt = program.UpdatedAt
        };
    }
}
