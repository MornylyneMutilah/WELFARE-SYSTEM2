namespace WelfareSystem.Api.Services.Interfaces;

public interface IWorkflowService
{
    Task<bool> CanTransitionStatusAsync(int currentStatus, int newStatus, string userRole);
    Task<bool> UpdateApplicationStatusAsync(int applicationId, int newStatus, string comments, int userId, string userRole);
}
