using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Common.Exceptions;
using TCIP.Infrastructure.Adapters.Recurrence;
using Xunit;

namespace TCIP.Infrastructure.IntegrationTests;

public sealed class RecurrenceEngineTests
{
    private readonly RecurrenceEngine engine = new();

    [Fact]
    public void NormalizeAndValidateRule_Weekly_CanonicalizesCorrectly()
    {
        var startUtc = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero); // Monday
        var rule = "FREQ=WEEKLY;BYDAY=MO,WE,FR";

        var canonical = engine.NormalizeAndValidateRule(rule, startUtc, "UTC");

        Assert.Equal("RRULE:FREQ=WEEKLY;BYDAY=MO,WE,FR", canonical);
    }

    [Fact]
    public void NormalizeAndValidateRule_WithRRulePrefix_CanonicalizesCorrectly()
    {
        var startUtc = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);
        var rule = "rrule:FREQ=WEEKLY;BYDAY=MO";

        var canonical = engine.NormalizeAndValidateRule(rule, startUtc, "UTC");

        Assert.Equal("RRULE:FREQ=WEEKLY;BYDAY=MO", canonical);
    }

    [Fact]
    public void NormalizeAndValidateRule_BiWeeklyInterval_ValidatesAndEvaluates()
    {
        var startUtc = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero); // Monday
        var rule = "FREQ=WEEKLY;INTERVAL=2;BYDAY=MO";

        var canonical = engine.NormalizeAndValidateRule(rule, startUtc, "UTC");
        Assert.Equal("RRULE:FREQ=WEEKLY;INTERVAL=2;BYDAY=MO", canonical);

        var next = engine.GetNextOccurrence(canonical, startUtc, null, "UTC", startUtc);
        Assert.NotNull(next);
        Assert.Equal(new DateTimeOffset(2026, 9, 21, 10, 0, 0, TimeSpan.Zero), next.StartAtUtc);
    }

    [Fact]
    public void NormalizeAndValidateRule_MonthlyDay15_ValidatesAndEvaluates()
    {
        var startUtc = new DateTimeOffset(2026, 9, 15, 14, 0, 0, TimeSpan.Zero);
        var rule = "FREQ=MONTHLY;BYMONTHDAY=15";

        var canonical = engine.NormalizeAndValidateRule(rule, startUtc, "UTC");
        Assert.Equal("RRULE:FREQ=MONTHLY;BYMONTHDAY=15", canonical);

        var next = engine.GetNextOccurrence(canonical, startUtc, null, "UTC", startUtc);
        Assert.NotNull(next);
        Assert.Equal(new DateTimeOffset(2026, 10, 15, 14, 0, 0, TimeSpan.Zero), next.StartAtUtc);
    }

    [Fact]
    public void NormalizeAndValidateRule_CountAndUntil_ValidatesCorrectly()
    {
        var startUtc = new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);
        var ruleCount = "FREQ=DAILY;COUNT=5";
        var ruleUntil = "FREQ=DAILY;UNTIL=20260930T235959Z";

        var canonicalCount = engine.NormalizeAndValidateRule(ruleCount, startUtc, "UTC");
        var canonicalUntil = engine.NormalizeAndValidateRule(ruleUntil, startUtc, "UTC");

        Assert.Equal("RRULE:FREQ=DAILY;COUNT=5", canonicalCount);
        Assert.Contains("FREQ=DAILY", canonicalUntil);
        Assert.Contains("UNTIL=", canonicalUntil);
    }

    [Fact]
    public void NormalizeAndValidateRule_Yearly_ValidatesCorrectly()
    {
        var startUtc = new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);
        var rule = "FREQ=YEARLY";

        var canonical = engine.NormalizeAndValidateRule(rule, startUtc, "UTC");
        Assert.Equal("RRULE:FREQ=YEARLY", canonical);

        var next = engine.GetNextOccurrence(canonical, startUtc, null, "UTC", startUtc);
        Assert.NotNull(next);
        Assert.Equal(new DateTimeOffset(2027, 9, 2, 8, 0, 0, TimeSpan.Zero), next.StartAtUtc);
    }

    [Fact]
    public void Recurrence_SparseOver20Years_EvaluatesCorrectlyWithout10YearLimit()
    {
        var startUtc = new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);
        var rule = "FREQ=YEARLY;INTERVAL=20";

        var canonical = engine.NormalizeAndValidateRule(rule, startUtc, "UTC");
        Assert.Equal("RRULE:FREQ=YEARLY;INTERVAL=20", canonical);

        var next = engine.GetNextOccurrence(canonical, startUtc, null, "UTC", startUtc);
        Assert.NotNull(next);
        Assert.Equal(new DateTimeOffset(2046, 9, 2, 8, 0, 0, TimeSpan.Zero), next.StartAtUtc);
    }

    [Fact]
    public void ResolveOriginalOccurrence_ValidOccurrence_ReturnsDetails()
    {
        var startUtc = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero); // Monday
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = startUtc,
            EndAtUtc = startUtc.AddHours(1),
            RecurrenceRule = "RRULE:FREQ=WEEKLY;BYDAY=MO",
            TimeZoneId = "UTC"
        };

        var nextMonday = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
        var resolved = engine.ResolveOriginalOccurrence(ev, nextMonday);

        Assert.NotNull(resolved);
        Assert.Equal(nextMonday, resolved.OriginalStartAtUtc);
        Assert.Equal(nextMonday, resolved.StartAtUtc);
    }

    [Fact]
    public void ResolveOriginalOccurrence_InvalidTimestamp_ReturnsNull()
    {
        var startUtc = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero); // Monday
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = startUtc,
            EndAtUtc = startUtc.AddHours(1),
            RecurrenceRule = "RRULE:FREQ=WEEKLY;BYDAY=MO",
            TimeZoneId = "UTC"
        };

        var tuesday = new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero);
        var resolved = engine.ResolveOriginalOccurrence(ev, tuesday);

        Assert.Null(resolved);
    }

    [Theory]
    [InlineData("FREQ=HOURLY")]
    [InlineData("FREQ=MINUTELY")]
    [InlineData("FREQ=SECONDLY")]
    [InlineData("FREQ=DAILY;BYHOUR=9")]
    [InlineData("FREQ=DAILY;BYMINUTE=30")]
    [InlineData("FREQ=DAILY;BYSECOND=0")]
    public void NormalizeAndValidateRule_SubDayRules_ThrowsBadRequestException(string invalidRule)
    {
        var startUtc = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);
        Assert.Throws<BadRequestException>(() =>
            engine.NormalizeAndValidateRule(invalidRule, startUtc, "UTC"));
    }

    [Fact]
    public void NormalizeAndValidateRule_DtStartMismatch_ThrowsBadRequestException()
    {
        var tuesdayStart = new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero);
        var rule = "FREQ=WEEKLY;BYDAY=MO,FR";

        var ex = Assert.Throws<BadRequestException>(() =>
            engine.NormalizeAndValidateRule(rule, tuesdayStart, "UTC"));
        Assert.Contains("DTSTART must be the first valid occurrence", ex.Message);
    }

    [Fact]
    public void Recurrence_DstTransition_PreservesLocalWallClockAcrossOffsets()
    {
        var nyTz = "America/New_York";
        var localTime = new DateTime(2026, 3, 7, 9, 0, 0);
        var startUtc = new DateTimeOffset(localTime, TimeSpan.FromHours(-5)).ToUniversalTime();

        var rule = "FREQ=DAILY;COUNT=3";
        var canonical = engine.NormalizeAndValidateRule(rule, startUtc, nyTz);

        var occ1 = engine.GetNextOccurrence(canonical, startUtc, null, nyTz, startUtc);
        Assert.NotNull(occ1);
        Assert.Equal(new DateTimeOffset(2026, 3, 8, 13, 0, 0, TimeSpan.Zero), occ1.StartAtUtc);

        var occ2 = engine.GetNextOccurrence(canonical, startUtc, null, nyTz, occ1.OriginalStartAtUtc);
        Assert.NotNull(occ2);
        Assert.Equal(new DateTimeOffset(2026, 3, 9, 13, 0, 0, TimeSpan.Zero), occ2.StartAtUtc);
    }
}
