using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Models;

namespace TCIP.Business.Modules.Calendar.Application.Ports;

public interface IRecurrenceEngine
{
    TimeZoneInfo GetTimeZone(string timeZoneId);
    string? NormalizeAndValidateRule(string? recurrenceRule, DateTimeOffset startAtUtc, string timeZoneId);
    OccurrenceDetails? GetNextOccurrence(
        string? recurrenceRule,
        DateTimeOffset startAtUtc,
        DateTimeOffset? endAtUtc,
        string timeZoneId,
        DateTimeOffset afterUtc,
        IReadOnlyList<EventOccurrenceException>? exceptions = null);
    IReadOnlyList<OccurrenceDetails> ExpandWindow(
        Event calendarEvent,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        IReadOnlyList<EventOccurrenceException> exceptions);
    OccurrenceDetails? ResolveOriginalOccurrence(Event calendarEvent, DateTimeOffset originalStartAtUtc);
}
