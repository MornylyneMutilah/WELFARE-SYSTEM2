using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class NotificationTriggerService
{
    private readonly INotificationService _notificationService;

    public NotificationTriggerService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task TriggerApplicationSubmittedAsync(int userId, int applicationId, string programName)
    {
        await _notificationService.CreateNotificationAsync(
            userId,
            "Application Submitted",
            $"Your application for {programName} has been submitted successfully and is pending review.",
            "Info",
            applicationId.ToString(),
            "Application"
        );
    }

    public async Task TriggerHRAssignmentAsync(int applicationId, string employeeName, string programName)
    {
        // This would notify HR officers - for now, we'll skip as we need to determine which HR to notify
        // In a real system, this would query for HR officers and notify them
    }

    public async Task TriggerStatusChangeAsync(int userId, int applicationId, string programName, string newStatus)
    {
        await _notificationService.CreateNotificationAsync(
            userId,
            "Application Status Updated",
            $"Your application for {programName} status has been updated to: {newStatus}",
            "Info",
            applicationId.ToString(),
            "Application"
        );
    }

    public async Task TriggerApprovalAsync(int userId, int applicationId, string programName)
    {
        await _notificationService.CreateNotificationAsync(
            userId,
            "Application Approved",
            $"Congratulations! Your application for {programName} has been approved.",
            "Success",
            applicationId.ToString(),
            "Application"
        );
    }

    public async Task TriggerRejectionAsync(int userId, int applicationId, string programName, string reason)
    {
        await _notificationService.CreateNotificationAsync(
            userId,
            "Application Rejected",
            $"Your application for {programName} has been rejected. Reason: {reason}",
            "Error",
            applicationId.ToString(),
            "Application"
        );
    }

    public async Task TriggerAdditionalInfoRequestAsync(int userId, int applicationId, string programName, string comment)
    {
        await _notificationService.CreateNotificationAsync(
            userId,
            "Additional Information Requested",
            $"Additional information has been requested for your {programName} application. Please review the details.",
            "Warning",
            applicationId.ToString(),
            "Application"
        );
    }
}
