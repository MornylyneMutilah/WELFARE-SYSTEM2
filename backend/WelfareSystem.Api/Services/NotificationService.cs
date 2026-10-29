using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Notification;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly AppDbContext _context;

    public NotificationService(
        INotificationRepository notificationRepository,
        AppDbContext context)
    {
        _notificationRepository = notificationRepository;
        _context = context;
    }

    public async Task<NotificationListResponse> GetUserNotificationsAsync(int userId)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(userId);
        var notificationList = notifications.OrderByDescending(n => n.CreatedAt).ToList();
        
        var unreadCount = await _notificationRepository.GetUnreadByUserIdAsync(userId);
        
        return new NotificationListResponse
        {
            Notifications = notificationList.Select(MapToResponse).ToList(),
            TotalCount = notificationList.Count,
            UnreadCount = unreadCount.Count()
        };
    }

    public async Task<NotificationListResponse> GetUnreadNotificationsAsync(int userId)
    {
        var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
        var notificationList = notifications.OrderByDescending(n => n.CreatedAt).ToList();
        
        return new NotificationListResponse
        {
            Notifications = notificationList.Select(MapToResponse).ToList(),
            TotalCount = notificationList.Count,
            UnreadCount = notificationList.Count
        };
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
        return notifications.Count();
    }

    public async Task<NotificationResponse?> CreateNotificationAsync(int userId, string title, string message, string type, string? entityId = null, string? entityType = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            EntityId = entityId,
            EntityType = entityType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        var createdNotification = await _notificationRepository.AddAsync(notification);
        return MapToResponse(createdNotification);
    }

    public async Task<NotificationResponse?> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        
        if (notification == null)
        {
            return null;
        }

        notification.IsRead = true;
        await _notificationRepository.UpdateAsync(notification);

        return MapToResponse(notification);
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
        
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification);
        }

        return true;
    }

    private static NotificationResponse MapToResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            EntityId = notification.EntityId,
            EntityType = notification.EntityType,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
