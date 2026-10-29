using WelfareSystem.Api.Models.DTOs.WelfareProgram;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IWelfareProgramService
{
    Task<WelfareProgramResponse?> GetProgramByIdAsync(int id);
    Task<IEnumerable<WelfareProgramResponse>> GetAllProgramsAsync();
    Task<IEnumerable<WelfareProgramResponse>> GetActiveProgramsAsync();
    Task<WelfareProgramResponse?> CreateProgramAsync(CreateWelfareProgramRequest request);
    Task<WelfareProgramResponse?> UpdateProgramAsync(int id, UpdateWelfareProgramRequest request);
    Task<bool> DeactivateProgramAsync(int id);
}
