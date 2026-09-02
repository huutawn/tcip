using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Models;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Tests.TestDoubles;

public sealed class SimpleTestRecurrenceEngine : IRecurrenceEngine
{
    public TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new BadRequestException("TimeZoneId is required.");
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    public string? NormalizeAndValidateRule(string? recurrenceRule, DateTimeOffset startAtUtc, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(recurrenceRule))
            return null;

        var clean = recurrenceRule.Trim();
        if (!clean.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
            clean = "RRULE:" + clean;

        if (clean.Contains("FREQ=HOURLY", StringComparison.OrdinalIgnoreCase) ||
            clean.Contains("FREQ=MINUTELY", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Sub-day rules not supported.");
        }

        return clean.ToUpperInvariant();
    }

    public OccurrenceDetails? GetNextOccurrence(
        string? recurrenceRule,
        DateTimeOffset startAtUtc,
        DateTimeOffset? endAtUtc,
        string timeZoneId,
        DateTimeOffset afterUtc,
        IReadOnlyList<EventOccurrenceException>? exceptions = null)
    {
        var duration = endAtUtc.HasValue ? endAtUtc.Value - startAtUtc : (TimeSpan?)null;

        if (string.IsNullOrWhiteSpace(recurrenceRule))
        {
            if (startAtUtc <= afterUtc)
                return null;
            return new OccurrenceDetails(startAtUtc, startAtUtc, endAtUtc);
        }

        var current = startAtUtc;
        while (current <= afterUtc)
        {
            if (recurrenceRule.Contains("FREQ=DAILY", StringComparison.OrdinalIgnoreCase))
            {
                current = current.AddDays(1);
            }
            else if (recurrenceRule.Contains("FREQ=WEEKLY", StringComparison.OrdinalIgnoreCase))
            {
                current = current.AddDays(7);
            }
            else
            {
                current = current.AddDays(1);
            }
        }

        return new OccurrenceDetails(current, current, duration.HasValue ? current + duration.Value : null);
    }

    public IReadOnlyList<OccurrenceDetails> ExpandWindow(
        Event calendarEvent,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        IReadOnlyList<EventOccurrenceException> exceptions)
    {
        var result = new List<OccurrenceDetails>();
        var duration = calendarEvent.EndAtUtc.HasValue ? calendarEvent.EndAtUtc.Value - calendarEvent.StartAtUtc : (TimeSpan?)null;

        var current = calendarEvent.StartAtUtc;
        while (current < windowEndUtc)
        {
            if (current >= windowStartUtc)
            {
                result.Add(new OccurrenceDetails(current, current, duration.HasValue ? current + duration.Value : null));
            }

            if (string.IsNullOrWhiteSpace(calendarEvent.RecurrenceRule))
                break;

            if (calendarEvent.RecurrenceRule.Contains("FREQ=DAILY", StringComparison.OrdinalIgnoreCase))
                current = current.AddDays(1);
            else
                current = current.AddDays(7);
        }

        return result;
    }

    public OccurrenceDetails? ResolveOriginalOccurrence(Event calendarEvent, DateTimeOffset originalStartAtUtc)
    {
        if (string.IsNullOrWhiteSpace(calendarEvent.RecurrenceRule))
        {
            if (calendarEvent.StartAtUtc == originalStartAtUtc)
            {
                var ex = calendarEvent.OccurrenceExceptions.FirstOrDefault(x => x.OriginalStartAtUtc == originalStartAtUtc);
                if (ex != null && ex.IsCancelled) return null;
                var effStart = ex?.StartAtUtc ?? originalStartAtUtc;
                var effEnd = ex?.EndAtUtc ?? calendarEvent.EndAtUtc;
                return new OccurrenceDetails(originalStartAtUtc, effStart, effEnd);
            }
            return null;
        }

        // For recurring events
        var diff = originalStartAtUtc - calendarEvent.StartAtUtc;
        if (diff < TimeSpan.Zero) return null;

        var exMatch = calendarEvent.OccurrenceExceptions.FirstOrDefault(x => x.OriginalStartAtUtc == originalStartAtUtc);
        if (exMatch != null && exMatch.IsCancelled) return null;

        var duration = calendarEvent.EndAtUtc.HasValue ? calendarEvent.EndAtUtc.Value - calendarEvent.StartAtUtc : (TimeSpan?)null;
        var start = exMatch?.StartAtUtc ?? originalStartAtUtc;
        var end = exMatch?.EndAtUtc ?? (duration.HasValue ? start + duration.Value : null);

        return new OccurrenceDetails(originalStartAtUtc, start, end);
    }
}
