using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Tests.TestDoubles;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class DispatchValidatorTests
{
    [Fact]
    public void DispatchValidator_CancelledEvent_ReturnsDrop()
    {
        var recurrence = new SimpleTestRecurrenceEngine();
        var validator = new ReminderDispatchValidator(recurrence);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = DateTimeOffset.UtcNow,
            TimeZoneId = "UTC",
            Status = EventStatus.Cancelled
        };
        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            Status = ReminderRuleStatus.Active
        };

        var result = validator.ValidateDispatch(
            ev,
            rule,
            ev.Id,
            rule.Id,
            ev.StartAtUtc,
            ev.StartAtUtc,
            ev.StartAtUtc.AddMinutes(-15),
            0);

        Assert.False(result.IsValid);
        Assert.Contains("cancelled", result.DropReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DispatchValidator_TimingChanged_ReturnsDrop()
    {
        var recurrence = new SimpleTestRecurrenceEngine();
        var validator = new ReminderDispatchValidator(recurrence);

        var origStart = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var movedStart = new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero);
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = origStart,
            TimeZoneId = "UTC",
            Status = EventStatus.Active
        };
        ev.OccurrenceExceptions.Add(new EventOccurrenceException
        {
            EventId = ev.Id,
            OriginalStartAtUtc = origStart,
            IsCancelled = false,
            StartAtUtc = movedStart
        });

        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RemindBeforeMinutes = 15,
            Status = ReminderRuleStatus.Active
        };

        var result = validator.ValidateDispatch(
            ev,
            rule,
            ev.Id,
            rule.Id,
            origStart,
            origStart, // Old timing
            origStart.AddMinutes(-15),
            0);

        Assert.False(result.IsValid);
        Assert.Contains("timing", result.DropReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DispatchValidator_ValidDispatch_ReturnsValid()
    {
        var recurrence = new SimpleTestRecurrenceEngine();
        var validator = new ReminderDispatchValidator(recurrence);

        var origStart = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = origStart,
            TimeZoneId = "UTC",
            Status = EventStatus.Active,
            Version = 3L
        };
        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RemindBeforeMinutes = 15,
            Status = ReminderRuleStatus.Active
        };

        var result = validator.ValidateDispatch(
            ev,
            rule,
            ev.Id,
            rule.Id,
            origStart,
            origStart,
            origStart.AddMinutes(-15),
            0);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Occurrence);
        Assert.Equal(origStart, result.Occurrence.OriginalStartAtUtc);
    }
}
