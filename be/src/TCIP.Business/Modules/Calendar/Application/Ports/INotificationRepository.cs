using TCIP.Business.Modules.Calendar.Domain.Entities;

namespace TCIP.Business.Modules.Calendar.Application.Ports;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetNotificationForUpdateAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
