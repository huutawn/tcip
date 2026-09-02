using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Models;
using TCIP.Common.Exceptions;

namespace TCIP.Infrastructure.Adapters.Recurrence;

public sealed class RecurrenceEngine : IRecurrenceEngine
{
    private const int MaxEvaluationLimit = 50_000;

    private static readonly HashSet<string> DisallowedSubDayTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "FREQ=HOURLY", "FREQ=MINUTELY", "FREQ=SECONDLY",
        "BYHOUR", "BYMINUTE", "BYSECOND"
    };

    public TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new BadRequestException("TimeZoneId is required.");

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (Exception)
        {
            throw new BadRequestException($"Invalid or unsupported timezone: '{timeZoneId}'.");
        }
    }

    public string? NormalizeAndValidateRule(string? recurrenceRule, DateTimeOffset startAtUtc, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(recurrenceRule))
            return null;

        var raw = recurrenceRule.Trim();
        var cleanRule = raw.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase)
            ? raw["RRULE:".Length..].Trim()
            : raw;

        foreach (var disallowed in DisallowedSubDayTokens)
        {
            if (cleanRule.Contains(disallowed, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException($"Recurrence rule contains unsupported sub-day element or modifier: '{disallowed}'.");
            }
        }

        RecurrencePattern pattern;
        try
        {
            pattern = new RecurrencePattern(cleanRule);
        }
        catch (Exception ex)
        {
            throw new BadRequestException($"Invalid RRULE format: {ex.Message}");
        }

        if (pattern.Frequency is not (FrequencyType.Daily or FrequencyType.Weekly or FrequencyType.Monthly or FrequencyType.Yearly))
        {
            throw new BadRequestException("Only DAILY, WEEKLY, MONTHLY, and YEARLY frequencies are supported.");
        }

        if (pattern.Interval < 1)
        {
            throw new BadRequestException("INTERVAL must be at least 1.");
        }

        if (pattern.Count < 0)
        {
            throw new BadRequestException("COUNT must be positive.");
        }

        var timeZone = GetTimeZone(timeZoneId);
        var localStart = TimeZoneInfo.ConvertTime(startAtUtc, timeZone).DateTime;

        var calEvent = new CalendarEvent
        {
            Start = new CalDateTime(localStart),
            RecurrenceRule = pattern
        };

        var firstOccurrences = calEvent.GetOccurrences(new CalDateTime(localStart)).Take(1).ToList();
        if (firstOccurrences.Count == 0 || firstOccurrences[0].Period.StartTime.Value != localStart)
        {
            throw new BadRequestException("DTSTART must be the first valid occurrence of the recurrence rule.");
        }

        var canonical = pattern.ToString()?.Trim() ?? string.Empty;
        return canonical.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase)
            ? canonical.ToUpperInvariant()
            : $"RRULE:{canonical.ToUpperInvariant()}";
    }

    public OccurrenceDetails? GetNextOccurrence(
        string? recurrenceRule,
        DateTimeOffset startAtUtc,
        DateTimeOffset? endAtUtc,
        string timeZoneId,
        DateTimeOffset afterUtc,
        IReadOnlyList<EventOccurrenceException>? exceptions = null)
    {
        var timeZone = GetTimeZone(timeZoneId);
        var exceptionMap = exceptions?.ToDictionary(x => x.OriginalStartAtUtc) ?? [];
        var masterDuration = endAtUtc.HasValue ? endAtUtc.Value - startAtUtc : (TimeSpan?)null;

        if (string.IsNullOrWhiteSpace(recurrenceRule))
        {
            if (startAtUtc <= afterUtc)
                return null;

            if (exceptionMap.TryGetValue(startAtUtc, out var singleEx))
            {
                if (singleEx.IsCancelled)
                    return null;

                var effStart = singleEx.StartAtUtc ?? startAtUtc;
                var effEnd = singleEx.EndAtUtc ?? (singleEx.StartAtUtc.HasValue && masterDuration.HasValue
                    ? effStart + masterDuration.Value
                    : endAtUtc);

                return new OccurrenceDetails(startAtUtc, effStart, effEnd);
            }

            return new OccurrenceDetails(startAtUtc, startAtUtc, endAtUtc);
        }

        var cleanRule = recurrenceRule.Trim();
        if (cleanRule.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
            cleanRule = cleanRule["RRULE:".Length..].Trim();

        var pattern = new RecurrencePattern(cleanRule);
        var localStart = TimeZoneInfo.ConvertTime(startAtUtc, timeZone).DateTime;

        var calEvent = new CalendarEvent
        {
            Start = new CalDateTime(localStart),
            RecurrenceRule = pattern
        };

        var localAfter = TimeZoneInfo.ConvertTime(afterUtc, timeZone).DateTime;
        var searchStart = localStart > localAfter ? localStart : localAfter.AddDays(-1);

        var count = 0;
        foreach (var occ in calEvent.GetOccurrences(new CalDateTime(searchStart)))
        {
            count++;
            if (count > MaxEvaluationLimit)
            {
                throw new BadRequestException("Recurrence evaluation limit exceeded.");
            }

            var occUtc = ToUtc(occ.Period.StartTime.Value, timeZone);
            if (occUtc <= afterUtc)
                continue;

            if (exceptionMap.TryGetValue(occUtc, out var ex))
            {
                if (ex.IsCancelled)
                    continue;

                var effStart = ex.StartAtUtc ?? occUtc;
                var effEnd = ex.EndAtUtc ?? (ex.StartAtUtc.HasValue && masterDuration.HasValue
                    ? effStart + masterDuration.Value
                    : (masterDuration.HasValue ? occUtc + masterDuration.Value : null));

                return new OccurrenceDetails(occUtc, effStart, effEnd);
            }

            var defaultEnd = masterDuration.HasValue ? occUtc + masterDuration.Value : (DateTimeOffset?)null;
            return new OccurrenceDetails(occUtc, occUtc, defaultEnd);
        }

        return null;
    }

    public IReadOnlyList<OccurrenceDetails> ExpandWindow(
        Event calendarEvent,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        IReadOnlyList<EventOccurrenceException> exceptions)
    {
        var timeZone = GetTimeZone(calendarEvent.TimeZoneId);
        var exceptionMap = exceptions.ToDictionary(x => x.OriginalStartAtUtc);
        var masterDuration = calendarEvent.EndAtUtc.HasValue
            ? calendarEvent.EndAtUtc.Value - calendarEvent.StartAtUtc
            : (TimeSpan?)null;

        var result = new Dictionary<DateTimeOffset, OccurrenceDetails>();

        if (string.IsNullOrWhiteSpace(calendarEvent.RecurrenceRule))
        {
            var origStart = calendarEvent.StartAtUtc;
            if (exceptionMap.TryGetValue(origStart, out var singleEx))
            {
                if (!singleEx.IsCancelled)
                {
                    var effStart = singleEx.StartAtUtc ?? origStart;
                    var effEnd = singleEx.EndAtUtc ?? (singleEx.StartAtUtc.HasValue && masterDuration.HasValue
                        ? effStart + masterDuration.Value
                        : calendarEvent.EndAtUtc);

                    if (Overlaps(effStart, effEnd, windowStartUtc, windowEndUtc))
                    {
                        result[origStart] = new OccurrenceDetails(origStart, effStart, effEnd);
                    }
                }
            }
            else
            {
                if (Overlaps(origStart, calendarEvent.EndAtUtc, windowStartUtc, windowEndUtc))
                {
                    result[origStart] = new OccurrenceDetails(origStart, origStart, calendarEvent.EndAtUtc);
                }
            }

            return result.Values
                .OrderBy(x => x.StartAtUtc)
                .ThenBy(_ => calendarEvent.Id)
                .ThenBy(x => x.OriginalStartAtUtc)
                .ToList();
        }

        var cleanRule = calendarEvent.RecurrenceRule.Trim();
        if (cleanRule.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
            cleanRule = cleanRule["RRULE:".Length..].Trim();

        var pattern = new RecurrencePattern(cleanRule);
        var localStart = TimeZoneInfo.ConvertTime(calendarEvent.StartAtUtc, timeZone).DateTime;

        var calEvent = new CalendarEvent
        {
            Start = new CalDateTime(localStart),
            RecurrenceRule = pattern
        };

        var leadDuration = masterDuration ?? TimeSpan.FromDays(2);
        if (leadDuration < TimeSpan.FromDays(2)) leadDuration = TimeSpan.FromDays(2);

        var searchWindowStartUtc = windowStartUtc - leadDuration;
        var searchWindowEndUtc = windowEndUtc + TimeSpan.FromDays(2);

        var localSearchStart = TimeZoneInfo.ConvertTime(searchWindowStartUtc, timeZone).DateTime;
        var localSearchEnd = TimeZoneInfo.ConvertTime(searchWindowEndUtc, timeZone).DateTime;

        var count = 0;
        foreach (var occ in calEvent.GetOccurrences(new CalDateTime(localSearchStart)))
        {
            count++;
            if (count > MaxEvaluationLimit)
            {
                throw new BadRequestException("Recurrence evaluation limit exceeded.");
            }

            var localOccTime = occ.Period.StartTime.Value;
            if (localOccTime > localSearchEnd)
            {
                break;
            }

            var origStartUtc = ToUtc(localOccTime, timeZone);

            if (exceptionMap.TryGetValue(origStartUtc, out var ex))
            {
                if (ex.IsCancelled)
                    continue;

                var effStart = ex.StartAtUtc ?? origStartUtc;
                var effEnd = ex.EndAtUtc ?? (ex.StartAtUtc.HasValue && masterDuration.HasValue
                    ? effStart + masterDuration.Value
                    : (masterDuration.HasValue ? origStartUtc + masterDuration.Value : null));

                if (Overlaps(effStart, effEnd, windowStartUtc, windowEndUtc))
                {
                    result[origStartUtc] = new OccurrenceDetails(origStartUtc, effStart, effEnd);
                }
            }
            else
            {
                var effEnd = masterDuration.HasValue ? origStartUtc + masterDuration.Value : (DateTimeOffset?)null;
                if (Overlaps(origStartUtc, effEnd, windowStartUtc, windowEndUtc))
                {
                    result[origStartUtc] = new OccurrenceDetails(origStartUtc, origStartUtc, effEnd);
                }
            }
        }

        foreach (var ex in exceptions)
        {
            if (ex.IsCancelled || !ex.StartAtUtc.HasValue)
                continue;

            if (result.ContainsKey(ex.OriginalStartAtUtc))
                continue;

            var effStart = ex.StartAtUtc.Value;
            var effEnd = ex.EndAtUtc ?? (masterDuration.HasValue ? effStart + masterDuration.Value : null);

            if (Overlaps(effStart, effEnd, windowStartUtc, windowEndUtc))
            {
                result[ex.OriginalStartAtUtc] = new OccurrenceDetails(ex.OriginalStartAtUtc, effStart, effEnd);
            }
        }

        return result.Values
            .OrderBy(x => x.StartAtUtc)
            .ThenBy(_ => calendarEvent.Id)
            .ThenBy(x => x.OriginalStartAtUtc)
            .ToList();
    }

    public OccurrenceDetails? ResolveOriginalOccurrence(Event calendarEvent, DateTimeOffset originalStartAtUtc)
    {
        var timeZone = GetTimeZone(calendarEvent.TimeZoneId);
        var masterDuration = calendarEvent.EndAtUtc.HasValue
            ? calendarEvent.EndAtUtc.Value - calendarEvent.StartAtUtc
            : (TimeSpan?)null;

        var ex = calendarEvent.OccurrenceExceptions.FirstOrDefault(x => x.OriginalStartAtUtc == originalStartAtUtc);
        if (ex != null && ex.IsCancelled)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(calendarEvent.RecurrenceRule))
        {
            if (calendarEvent.StartAtUtc != originalStartAtUtc)
                return null;

            var effStart = ex?.StartAtUtc ?? originalStartAtUtc;
            var effEnd = ex?.EndAtUtc ?? (ex?.StartAtUtc.HasValue == true && masterDuration.HasValue
                ? effStart + masterDuration.Value
                : calendarEvent.EndAtUtc);

            return new OccurrenceDetails(originalStartAtUtc, effStart, effEnd);
        }

        var cleanRule = calendarEvent.RecurrenceRule.Trim();
        if (cleanRule.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
            cleanRule = cleanRule["RRULE:".Length..].Trim();

        var pattern = new RecurrencePattern(cleanRule);
        var localStart = TimeZoneInfo.ConvertTime(calendarEvent.StartAtUtc, timeZone).DateTime;

        var calEvent = new CalendarEvent
        {
            Start = new CalDateTime(localStart),
            RecurrenceRule = pattern
        };

        var targetLocal = TimeZoneInfo.ConvertTime(originalStartAtUtc, timeZone).DateTime;
        var searchRangeStart = targetLocal.AddDays(-1);
        var searchRangeEnd = targetLocal.AddDays(1);

        var matches = calEvent.GetOccurrences(new CalDateTime(searchRangeStart))
            .TakeWhile(occ => occ.Period.StartTime.Value <= searchRangeEnd)
            .Select(occ => ToUtc(occ.Period.StartTime.Value, timeZone))
            .Any(utc => utc == originalStartAtUtc);

        if (!matches)
        {
            return null;
        }

        var effectiveStart = ex?.StartAtUtc ?? originalStartAtUtc;
        var effectiveEnd = ex?.EndAtUtc ?? (ex?.StartAtUtc.HasValue == true && masterDuration.HasValue
            ? effectiveStart + masterDuration.Value
            : (masterDuration.HasValue ? originalStartAtUtc + masterDuration.Value : null));

        return new OccurrenceDetails(originalStartAtUtc, effectiveStart, effectiveEnd);
    }

    private static bool Overlaps(
        DateTimeOffset eventStartUtc,
        DateTimeOffset? eventEndUtc,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc)
    {
        if (eventEndUtc.HasValue)
        {
            return eventStartUtc < windowEndUtc && eventEndUtc.Value > windowStartUtc;
        }

        return eventStartUtc >= windowStartUtc && eventStartUtc < windowEndUtc;
    }

    private static DateTimeOffset ToUtc(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        if (timeZone.IsInvalidTime(localDateTime))
        {
            var adjusted = localDateTime;
            for (var i = 1; i <= 8; i++)
            {
                adjusted = localDateTime.AddMinutes(15 * i);
                if (!timeZone.IsInvalidTime(adjusted))
                {
                    localDateTime = adjusted;
                    break;
                }
            }
        }

        if (timeZone.IsAmbiguousTime(localDateTime))
        {
            var offsets = timeZone.GetAmbiguousTimeOffsets(localDateTime);
            var standardOffset = offsets.Min();
            return new DateTimeOffset(localDateTime, standardOffset).ToUniversalTime();
        }

        var offset = timeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }
}
