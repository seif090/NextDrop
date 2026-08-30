namespace NextDrop.Modules.Notifications.Application.DTOs;

public record NotificationDto(
    Guid Id,
    Guid UserId,
    string Type,
    string Title,
    string Body,
    string? DataJson,
    string Channel,
    string Priority,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public record UserNotificationPreferenceDto(
    Guid UserId,
    bool AllowOrderNotifications,
    bool AllowMarketingNotifications,
    bool EmailEnabled,
    bool InAppEnabled);

public record UnreadNotificationCountDto(
    int UnreadCount);

public record PagedNotificationResultDto(
    List<NotificationDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
