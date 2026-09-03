using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;

namespace TCIP.Business.Modules.Calendar.Domain.Services;

public static class CalendarResponseMapper
{
    public static CalendarEventDetailResponse MapDetail(Event ev) => new(
        ev.Id,
        ev.CreatedById,
        ev.StartAtUtc,
        ev.EndAtUtc,
        ev.TimeZoneId,
        ev.RecurrenceRule,
        ev.Status,
        ev.Version,
        ev.Translations.Select(t => new EventTranslationResponse(t.Language, t.Title, t.Description)).ToList(),
        ev.Audiences.Where(a => a.Status == EventAudienceStatus.Active).Select(a => new EventAudienceResponse(
            a.PrincipalId,
            a.Principal?.Type.ToString(),
            a.Principal?.User?.DisplayName ?? a.Principal?.Group?.Name ?? a.Principal?.Team?.Name ?? a.Principal?.Project?.Name ?? a.Principal?.Department?.Name,
            a.Status)).ToList(),
        ev.ReminderRules.Where(r => r.Status == ReminderRuleStatus.Active).Select(r => new ReminderRuleResponse(
            r.Id,
            r.EventId,
            r.RemindBeforeMinutes,
            r.RepeatEveryMinutes,
            r.RepeatCount,
            r.MisfirePolicy,
            r.MaxLatenessMinutes,
            r.Status,
            r.CreatedAtUtc,
            r.UpdatedAtUtc)).ToList(),
        ev.OccurrenceExceptions.Select(ex => new OccurrenceExceptionResponse(
            ex.OriginalStartAtUtc,
            ex.IsCancelled,
            ex.StartAtUtc,
            ex.EndAtUtc,
            ex.UpdatedAtUtc)).ToList(),
        ev.CreatedAtUtc,
        ev.UpdatedAtUtc);

    public static ReminderRuleResponse MapRule(ReminderRule r) => new(
        r.Id,
        r.EventId,
        r.RemindBeforeMinutes,
        r.RepeatEveryMinutes,
        r.RepeatCount,
        r.MisfirePolicy,
        r.MaxLatenessMinutes,
        r.Status,
        r.CreatedAtUtc,
        r.UpdatedAtUtc);

    public static OccurrenceExceptionResponse MapException(EventOccurrenceException ex) => new(
        ex.OriginalStartAtUtc,
        ex.IsCancelled,
        ex.StartAtUtc,
        ex.EndAtUtc,
        ex.UpdatedAtUtc);

    public static NotificationResponse MapNotification(Notification n) => new(
        n.Id,
        n.EventId,
        n.ReminderRuleId,
        n.OriginalStartAtUtc,
        n.EffectiveStartAtUtc,
        n.ScheduledFireAtUtc,
        n.RepeatIndex,
        n.Title,
        n.Description,
        n.SentAtUtc,
        n.ReadAtUtc);
}
