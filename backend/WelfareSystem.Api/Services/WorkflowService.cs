using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWelfareApplicationRepository _applicationRepository;
    private readonly IStatusHistoryService _statusHistoryService;

    public WorkflowService(
        IWelfareApplicationRepository applicationRepository,
        IStatusHistoryService statusHistoryService)
    {
        _applicationRepository = applicationRepository;
        _statusHistoryService = statusHistoryService;
    }

    public Task<bool> CanTransitionStatusAsync(int currentStatus, int newStatus, string userRole)
    {
        // Define valid status transitions based on user role
        var validTransitions = GetValidTransitions(userRole);
        
        return Task.FromResult(validTransitions.Contains(newStatus));
    }

    public async Task<bool> UpdateApplicationStatusAsync(int applicationId, int newStatus, string comments, int userId, string userRole)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null)
        {
            return false;
        }

        // Check if transition is valid
        if (!await CanTransitionStatusAsync(application.Status, newStatus, userRole))
        {
            return false;
        }

        // Update application status
        application.Status = newStatus;
        application.UpdatedAt = DateTime.UtcNow;
        await _applicationRepository.UpdateAsync(application);

        // Add status history
        await _statusHistoryService.AddStatusHistoryAsync(applicationId, newStatus, userId, comments);

        return true;
    }

    private List<int> GetValidTransitions(string userRole)
    {
        // Status values:
        // 1: Draft, 2: Submitted, 3: Under HR Review, 4: Under Committee Review
        // 5: Approved by Committee, 6: Rejected by Committee, 7: Pending Management Approval
        // 8: Approved, 9: Rejected, 10: Cancelled

        return userRole.ToLower() switch
        {
            "hrofficer" => new List<int> { 3, 9 }, // Can move to HR Review or Reject
            "committeemember" => new List<int> { 4, 5, 6 }, // Can move to Committee Review, Approve, or Reject
            "management" => new List<int> { 7, 8, 9 }, // Can move to Pending Management, Approve, or Reject
            "administrator" => new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, // Admin can set any status
            _ => new List<int>()
        };
    }
}
