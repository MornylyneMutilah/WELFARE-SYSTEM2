using WelfareSystem.Api.Models.DTOs.Application;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IWelfareApplicationService
{
    Task<ApplicationResponse?> GetApplicationByIdAsync(int id);
    Task<IEnumerable<ApplicationResponse>> GetApplicationsByUserIdAsync(int userId);
    Task<IEnumerable<ApplicationResponse>> GetAllApplicationsAsync();
    Task<ApplicationResponse?> CreateApplicationAsync(int userId, CreateApplicationRequest request);
    Task<ApplicationResponse?> UpdateApplicationAsync(int id, UpdateApplicationRequest request);
    Task<ApplicationResponse?> SubmitApplicationAsync(int id);
    Task<bool> CancelApplicationAsync(int id);
}
