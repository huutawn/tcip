using System.Text;
using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Occurrences;

public interface IGetEventsByDayUseCase
{
    Task<CalendarEventsByDayResponse> GetEventsByDayAsync(Guid userId, DateTimeOffset? day, string? cursor, int limit = 200, CancellationToken cancellationToken = default);
}

public sealed class GetEventsByDayUseCase(
    ICalendarDayQueryRepository calendarDayQueryRepository,
    IRecurrenceEngine recurrenceEngine,
    IUserPrincipalLookupQuery userPrincipalLookupQuery,
    TimeProvider timeProvider) : IGetEventsByDayUseCase
{
    public async Task<CalendarEventsByDayResponse> GetEventsByDayAsync(
        Guid userId,
        DateTimeOffset? day,
        string? cursor,
        int limit = 200,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000)
        {
            limit = Math.Clamp(limit, 1, 1000);
        }

        var user = await userPrincipalLookupQuery.FindByIdAsync(userId, cancellationToken);
        var userTz = recurrenceEngine.GetTimeZone(user?.TimeZoneId ?? "UTC");
        var userLanguage = user?.Language ?? "en";

        var refTime = day ?? timeProvider.GetUtcNow();
        var userLocalRef = TimeZoneInfo.ConvertTime(refTime, userTz).DateTime;

        var localMidnightStart = new DateTime(userLocalRef.Year, userLocalRef.Month, userLocalRef.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localMidnightEnd = localMidnightStart.AddDays(1);

        var dayStartUtc = ToUtc(localMidnightStart, userTz);
        var dayEndUtc = ToUtc(localMidnightEnd, userTz);

        var events = await calendarDayQueryRepository.GetEventsForDayWindowAsync(userId, dayStartUtc, dayEndUtc, cancellationToken);
        var allOccurrences = new List<EventOccurrenceResponse>();

        foreach (var ev in events)
        {
            var occurrences = recurrenceEngine.ExpandWindow(
                ev,
                dayStartUtc,
                dayEndUtc,
                ev.OccurrenceExceptions.ToList());

            var (title, description) = SelectTranslation(ev.Translations, userLanguage);

            foreach (var occ in occurrences)
            {
                allOccurrences.Add(new EventOccurrenceResponse(
                    ev.Id,
                    occ.OriginalStartAtUtc,
                    occ.StartAtUtc,
                    occ.EndAtUtc,
                    title,
                    description,
                    ev.TimeZoneId,
                    ev.Status,
                    ev.Version));
            }
        }

        var sorted = allOccurrences
            .OrderBy(x => x.StartAt)
            .ThenBy(x => x.EventId)
            .ThenBy(x => x.OriginalStartAt)
            .ToList();

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            var parsed = DecodeCursor(cursor);
            sorted = sorted
                .Where(x => x.StartAt > parsed.StartAt ||
                            (x.StartAt == parsed.StartAt && x.EventId.CompareTo(parsed.EventId) > 0) ||
                            (x.StartAt == parsed.StartAt && x.EventId == parsed.EventId && x.OriginalStartAt > parsed.OriginalStartAt))
                .ToList();
        }

        var hasNextPage = sorted.Count > limit;
        var pagedItems = sorted.Take(limit).ToList();
        var nextCursor = hasNextPage && pagedItems.Count > 0
            ? EncodeCursor(pagedItems[^1])
            : null;

        return new CalendarEventsByDayResponse(pagedItems, nextCursor);
    }

    private static (string Title, string? Description) SelectTranslation(
        ICollection<EventTranslation> translations,
        string userLanguage)
    {
        if (translations.Count == 0)
            return (string.Empty, null);

        var exact = translations.FirstOrDefault(t => string.Equals(t.Language, userLanguage, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return (exact.Title, exact.Description);

        var baseUserLang = userLanguage.Split('-')[0];
        var baseMatch = translations.FirstOrDefault(t => string.Equals(t.Language.Split('-')[0], baseUserLang, StringComparison.OrdinalIgnoreCase));
        if (baseMatch is not null)
            return (baseMatch.Title, baseMatch.Description);

        var english = translations.FirstOrDefault(t => string.Equals(t.Language, "en", StringComparison.OrdinalIgnoreCase));
        if (english is not null)
            return (english.Title, english.Description);

        var first = translations.First();
        return (first.Title, first.Description);
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

    private static string EncodeCursor(EventOccurrenceResponse occ)
    {
        var raw = $"v1|{occ.StartAt.ToUnixTimeMilliseconds()}|{occ.EventId:N}|{occ.OriginalStartAt.ToUnixTimeMilliseconds()}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTimeOffset StartAt, Guid EventId, DateTimeOffset OriginalStartAt) DecodeCursor(string cursor)
    {
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|');
            if (parts.Length != 4 || parts[0] != "v1")
            {
                throw new BadRequestException("Invalid cursor format.");
            }

            var startAtMs = long.Parse(parts[1]);
            var eventId = Guid.Parse(parts[2]);
            var origStartAtMs = long.Parse(parts[3]);

            return (
                DateTimeOffset.FromUnixTimeMilliseconds(startAtMs),
                eventId,
                DateTimeOffset.FromUnixTimeMilliseconds(origStartAtMs));
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            throw new BadRequestException("Invalid cursor.");
        }
    }
}
