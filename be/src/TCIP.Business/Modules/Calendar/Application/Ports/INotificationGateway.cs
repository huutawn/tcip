using TCIP.Business.Modules.Calendar.Application.Contracts;

namespace TCIP.Business.Modules.Calendar.Application.Ports;

public interface INotificationGateway
{
    Task SendNotificationToUserAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken = default);
}
