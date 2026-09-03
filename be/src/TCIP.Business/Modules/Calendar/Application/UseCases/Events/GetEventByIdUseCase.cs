using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Events;

public interface IGetEventByIdUseCase
{
    Task<CalendarEventDetailResponse> GetEventDetailAsync(Guid eventId, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class GetEventByIdUseCase(
    IEventRepository eventRepository,
    IUserPrincipalLookupQuery userPrincipalLookupQuery) : IGetEventByIdUseCase
{
    public async Task<CalendarEventDetailResponse> GetEventDetailAsync(
        Guid eventId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await eventRepository.GetEventByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        var user = await userPrincipalLookupQuery.FindByIdAsync(actorUserId, cancellationToken);
        var isCreator = calendarEvent.CreatedById == actorUserId;
        var isAudience = user != null && calendarEvent.Audiences.Any(a =>
            a.Status == EventAudienceStatus.Active &&
            (a.PrincipalId == user.PrincipalId ||
             (a.Principal != null && a.Principal.Type != PrincipalType.User)));

        if (!isCreator && !isAudience)
        {
            throw new NotFoundException($"Event with ID '{eventId}' not found.");
        }

        return CalendarResponseMapper.MapDetail(calendarEvent);
    }
}
