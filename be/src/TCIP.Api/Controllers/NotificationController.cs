using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Api.Security;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases.Notifications;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar/notifications")]
public sealed class NotificationController(
    IGetNotificationsUseCase getNotificationsUseCase,
    IMarkNotificationReadUseCase markNotificationReadUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetNotificationsAsync(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var list = await getNotificationsUseCase.GetNotificationsAsync(userId, cancellationToken);
        return Ok(list);
    }

    [HttpPut("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkNotificationReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        return await markNotificationReadUseCase.MarkNotificationReadAsync(userId, notificationId, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
