using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;

namespace TCIP.Infrastructure.Adapters.Hub;

public sealed class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
{
}

public static class NotificationHubMethods
{
    public const string Notification = "notification";
}

public sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue("sub")
            ?? connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

public sealed class SignalRNotificationGateway(IHubContext<NotificationHub> hubContext) : INotificationGateway
{
    public Task SendNotificationToUserAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients
            .User(userId.ToString("D"))
            .SendAsync(NotificationHubMethods.Notification, notification, cancellationToken);
    }
}
