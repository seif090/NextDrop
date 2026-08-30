using MediatR;
using NextDrop.Modules.Notifications.Application.Abstractions;
using NextDrop.Modules.Notifications.Application.Commands;
using NextDrop.Modules.Notifications.Application.DTOs;
using NextDrop.Modules.Notifications.Domain.Aggregates;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Application.Queries;

public record GetNotificationsQuery(Guid RequesterUserId, int Page = 1, int PageSize = 20) : IRequest<Result<PagedNotificationResultDto>>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<PagedNotificationResultDto>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<PagedNotificationResultDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _notificationRepository.GetPagedByUserIdAsync(request.RequesterUserId, request.Page, request.PageSize, cancellationToken);
        var dtos = items.Select(CreateNotificationCommandHandler.MapToDto).ToList();

        var pagedResult = new PagedNotificationResultDto(dtos, request.Page, request.PageSize, totalCount);
        return pagedResult;
    }
}

public record GetUnreadNotificationsQuery(Guid RequesterUserId) : IRequest<Result<List<NotificationDto>>>;

public class GetUnreadNotificationsQueryHandler : IRequestHandler<GetUnreadNotificationsQuery, Result<List<NotificationDto>>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetUnreadNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<List<NotificationDto>>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
    {
        var unreadNotifications = await _notificationRepository.GetUnreadByUserIdAsync(request.RequesterUserId, cancellationToken);
        var dtos = unreadNotifications.Select(CreateNotificationCommandHandler.MapToDto).ToList();
        return dtos;
    }
}

public record GetNotificationPreferencesQuery(Guid RequesterUserId) : IRequest<Result<UserNotificationPreferenceDto>>;

public class GetNotificationPreferencesQueryHandler : IRequestHandler<GetNotificationPreferencesQuery, Result<UserNotificationPreferenceDto>>
{
    private readonly IUserNotificationPreferenceRepository _preferenceRepository;

    public GetNotificationPreferencesQueryHandler(IUserNotificationPreferenceRepository preferenceRepository)
    {
        _preferenceRepository = preferenceRepository;
    }

    public async Task<Result<UserNotificationPreferenceDto>> Handle(GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var pref = await _preferenceRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (pref == null)
        {
            return new UserNotificationPreferenceDto(
                request.RequesterUserId,
                AllowOrderNotifications: true,
                AllowMarketingNotifications: false,
                EmailEnabled: true,
                InAppEnabled: true);
        }

        return new UserNotificationPreferenceDto(
            pref.UserId,
            pref.AllowOrderNotifications,
            pref.AllowMarketingNotifications,
            pref.EmailEnabled,
            pref.InAppEnabled);
    }
}
