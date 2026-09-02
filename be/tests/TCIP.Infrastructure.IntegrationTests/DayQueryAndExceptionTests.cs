using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Infrastructure.Adapters.Recurrence;
using Xunit;

namespace TCIP.Infrastructure.IntegrationTests;

public sealed class DayQueryAndExceptionTests
{
    private readonly RecurrenceEngine engine = new();

    [Fact]
    public void ExpandWindow_OverlappingEvents_IncludesSpanStartingBeforeDay()
    {
        var dayStart = new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var crossingEvent = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero),
            EndAtUtc = new DateTimeOffset(2026, 9, 2, 4, 0, 0, TimeSpan.Zero),
            TimeZoneId = "UTC"
        };

        var occurrences = engine.ExpandWindow(crossingEvent, dayStart, dayEnd, []);

        Assert.Single(occurrences);
        Assert.Equal(crossingEvent.StartAtUtc, occurrences[0].StartAtUtc);
        Assert.Equal(crossingEvent.EndAtUtc, occurrences[0].EndAtUtc);
    }

    [Fact]
    public void ExpandWindow_InstantEvent_IncludedOnlyIfStartWithinDay()
    {
        var dayStart = new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var instantToday = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero),
            EndAtUtc = null,
            TimeZoneId = "UTC"
        };

        var instantYesterday = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = new DateTimeOffset(2026, 9, 1, 23, 59, 0, TimeSpan.Zero),
            EndAtUtc = null,
            TimeZoneId = "UTC"
        };

        var occToday = engine.ExpandWindow(instantToday, dayStart, dayEnd, []);
        var occYesterday = engine.ExpandWindow(instantYesterday, dayStart, dayEnd, []);

        Assert.Single(occToday);
        Assert.Empty(occYesterday);
    }

    [Fact]
    public void ExpandWindow_CancelledException_OmitsOccurrence()
    {
        var dayStart = new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var startUtc = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var dailyEvent = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = startUtc,
            EndAtUtc = startUtc.AddHours(1),
            RecurrenceRule = "RRULE:FREQ=DAILY",
            TimeZoneId = "UTC"
        };

        var sept2Original = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var cancelledEx = new EventOccurrenceException
        {
            EventId = dailyEvent.Id,
            OriginalStartAtUtc = sept2Original,
            IsCancelled = true
        };

        var occurrences = engine.ExpandWindow(dailyEvent, dayStart, dayEnd, [cancelledEx]);

        Assert.Empty(occurrences);
    }

    [Fact]
    public void ExpandWindow_OverrideStartWithoutEnd_PreservesMasterDuration()
    {
        var dayStart = new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var startUtc = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var masterDuration = TimeSpan.FromHours(2);
        var dailyEvent = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = startUtc,
            EndAtUtc = startUtc + masterDuration,
            RecurrenceRule = "RRULE:FREQ=DAILY",
            TimeZoneId = "UTC"
        };

        var sept2Original = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var movedStart = new DateTimeOffset(2026, 9, 2, 15, 0, 0, TimeSpan.Zero);
        var overrideEx = new EventOccurrenceException
        {
            EventId = dailyEvent.Id,
            OriginalStartAtUtc = sept2Original,
            IsCancelled = false,
            StartAtUtc = movedStart,
            EndAtUtc = null
        };

        var occurrences = engine.ExpandWindow(dailyEvent, dayStart, dayEnd, [overrideEx]);

        Assert.Single(occurrences);
        Assert.Equal(sept2Original, occurrences[0].OriginalStartAtUtc);
        Assert.Equal(movedStart, occurrences[0].StartAtUtc);
        Assert.Equal(movedStart + masterDuration, occurrences[0].EndAtUtc);
    }

    [Fact]
    public void ExpandWindow_ExceptionShiftedIntoDay_IsIncluded()
    {
        var dayStart = new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var monday = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);
        var weeklyEvent = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = monday,
            EndAtUtc = monday.AddHours(1),
            RecurrenceRule = "RRULE:FREQ=WEEKLY;BYDAY=MO",
            TimeZoneId = "UTC"
        };

        var shiftedIntoDay = new EventOccurrenceException
        {
            EventId = weeklyEvent.Id,
            OriginalStartAtUtc = monday,
            IsCancelled = false,
            StartAtUtc = new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero),
            EndAtUtc = new DateTimeOffset(2026, 9, 2, 15, 0, 0, TimeSpan.Zero)
        };

        var occurrences = engine.ExpandWindow(weeklyEvent, dayStart, dayEnd, [shiftedIntoDay]);

        Assert.Single(occurrences);
        Assert.Equal(monday, occurrences[0].OriginalStartAtUtc);
        Assert.Equal(shiftedIntoDay.StartAtUtc, occurrences[0].StartAtUtc);
    }

    [Fact]
    public void ExpandWindow_ExceptionShiftedOutOfDay_IsNotIncludedInOriginalDay()
    {
        var dayStart = new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var startUtc = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var dailyEvent = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = startUtc,
            EndAtUtc = startUtc.AddHours(1),
            RecurrenceRule = "RRULE:FREQ=DAILY",
            TimeZoneId = "UTC"
        };

        var sept2Original = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var shiftedOutOfDay = new EventOccurrenceException
        {
            EventId = dailyEvent.Id,
            OriginalStartAtUtc = sept2Original,
            IsCancelled = false,
            StartAtUtc = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero),
            EndAtUtc = new DateTimeOffset(2026, 9, 10, 11, 0, 0, TimeSpan.Zero)
        };

        var occurrences = engine.ExpandWindow(dailyEvent, dayStart, dayEnd, [shiftedOutOfDay]);

        Assert.Empty(occurrences);
    }
}
