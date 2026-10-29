using WelfareSystem.Api.Models.DTOs.Notification;

namespace WelfareSystem.Api.Services.Interfaces;

public interface INotificationService
{
    Task<NotificationListResponse> GetUserNotificationsAsync(int userId);
    Task<NotificationListResponse> GetUnreadNotificationsAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<NotificationResponse?> CreateNotificationAsync(int userId, string title, string message, string type, string? entityId = null, string? entityType = null);
    Task<NotificationResponse?> MarkAsReadAsync(int notificationId, int userId);
    Task<bool> MarkAllAsReadAsync(int userId);
}
