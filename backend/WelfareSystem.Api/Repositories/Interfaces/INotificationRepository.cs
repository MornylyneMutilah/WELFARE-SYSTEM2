using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface INotificationRepository : IBaseRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(int userId);
}
