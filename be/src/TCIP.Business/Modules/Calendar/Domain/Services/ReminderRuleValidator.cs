using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Domain.Services;

public static class ReminderRuleValidator
{
    public static void Validate(
        int remindBeforeMinutes,
        int? repeatEveryMinutes,
        int repeatCount,
        int maxLatenessMinutes)
    {
        if (remindBeforeMinutes < 0)
            throw new BadRequestException("RemindBeforeMinutes cannot be negative.");
        if (repeatCount < 0)
            throw new BadRequestException("RepeatCount cannot be negative.");
        if (maxLatenessMinutes < 0)
            throw new BadRequestException("MaxLatenessMinutes cannot be negative.");

        if (repeatCount > 0)
        {
            if (!repeatEveryMinutes.HasValue || repeatEveryMinutes.Value <= 0)
                throw new BadRequestException("RepeatEveryMinutes is required and must be positive when RepeatCount > 0.");

            var totalRepeatMinutes = (long)repeatCount * repeatEveryMinutes.Value;
            if (totalRepeatMinutes >= remindBeforeMinutes)
                throw new BadRequestException("The last reminder repeat must fire strictly before the event occurrence start.");
        }
    }
}
