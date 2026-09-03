using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Services;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Notifications;

public interface IGetNotificationsUseCase
{
    Task<IReadOnlyList<NotificationResponse>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class GetNotificationsUseCase(INotificationRepository notificationRepository) : IGetNotificationsUseCase
{
    public async Task<IReadOnlyList<NotificationResponse>> GetNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var list = await notificationRepository.GetNotificationsAsync(userId, cancellationToken);
        return list.Select(CalendarResponseMapper.MapNotification).ToList();
    }
}
