using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Calendar;

public sealed class NotificationRepository(TcipDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.SentAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Notification?> GetNotificationForUpdateAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.SingleOrDefaultAsync(
            x => x.Id == notificationId && x.RecipientUserId == userId,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
