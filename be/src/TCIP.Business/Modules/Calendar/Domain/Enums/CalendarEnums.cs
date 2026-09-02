namespace TCIP.Business.Modules.Calendar.Domain.Enums;

public enum EventStatus
{
    Active,
    Cancelled
}

public enum EventAudienceStatus
{
    Active,
    Removed
}

public enum ReminderRuleStatus
{
    Active,
    Cancelled,
    Completed
}

public enum ReminderScheduleStatus
{
    Active,
    Cancelled,
    Completed
}

public enum MisfirePolicy
{
    Skip,
    FireOnceNow,
    CatchUp
}

public enum OutboxMessageStatus
{
    Pending,
    Publishing,
    Published
}
