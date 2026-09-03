using TCIP.Business.Modules.Calendar.Application.Ports;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Notifications;

public interface IMarkNotificationReadUseCase
{
    Task<bool> MarkNotificationReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
}

public sealed class MarkNotificationReadUseCase(
    INotificationRepository notificationRepository,
    TimeProvider timeProvider) : IMarkNotificationReadUseCase
{
    public async Task<bool> MarkNotificationReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await notificationRepository.GetNotificationForUpdateAsync(userId, notificationId, cancellationToken);
        if (notification is null)
        {
            return false;
        }

        if (!notification.ReadAtUtc.HasValue)
        {
            notification.ReadAtUtc = timeProvider.GetUtcNow();
            await notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
