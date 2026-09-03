using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Domain.Services;

public static class ReminderRuleValidator
{
    public static void Validate(
        int remindBeforeMinutes,
        int? repeatEveryMinutes,
        int maxLatenessMinutes)
    {
        if (remindBeforeMinutes < 0)
            throw new BadRequestException("RemindBeforeMinutes cannot be negative.");
        if (maxLatenessMinutes < 0)
            throw new BadRequestException("MaxLatenessMinutes cannot be negative.");

        if (repeatEveryMinutes.HasValue && repeatEveryMinutes.Value <= 0)
        {
            throw new BadRequestException("RepeatEveryMinutes must be positive when specified.");
        }
    }
}
