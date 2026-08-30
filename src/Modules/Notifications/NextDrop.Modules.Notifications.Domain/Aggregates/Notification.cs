using NextDrop.Modules.Notifications.Domain.Entities;
using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.Modules.Notifications.Domain.Events;
using NextDrop.Modules.Notifications.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Domain.Aggregates;

public class Notification : AggregateRoot<NotificationId>
{
    private readonly List<NotificationDelivery> _deliveries = new();

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? DataJson { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationPriority Priority { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public IReadOnlyCollection<NotificationDelivery> Deliveries => _deliveries.AsReadOnly();

    private Notification() { } // EF Core

    private Notification(
        NotificationId id,
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? dataJson,
        NotificationChannel channel,
        NotificationPriority priority,
        DateTimeOffset now,
        DateTimeOffset? expiresAtUtc = null)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Title = title.Trim();
        Body = body.Trim();
        DataJson = dataJson;
        Channel = channel;
        Priority = priority;
        Status = NotificationStatus.Unread;
        CreatedAtUtc = now;
        ExpiresAtUtc = expiresAtUtc;
    }

    public static Result<Notification> Create(
        NotificationId id,
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? dataJson,
        NotificationChannel channel,
        NotificationPriority priority,
        DateTimeOffset now,
        DateTimeOffset? expiresAtUtc = null)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Notification>(Error.Validation("Notification.EmptyUser", "UserId is required."));

        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<Notification>(Error.Validation("Notification.EmptyTitle", "Notification title is required."));

        if (string.IsNullOrWhiteSpace(body))
            return Result.Failure<Notification>(Error.Validation("Notification.EmptyBody", "Notification body is required."));

        var notification = new Notification(id, userId, type, title, body, dataJson, channel, priority, now, expiresAtUtc);
        notification._deliveries.Add(new NotificationDelivery(NotificationDeliveryId.New(), id, channel, now));

        notification.AddDomainEvent(new NotificationCreatedDomainEvent(id, userId, type, channel));
        return notification;
    }

    public Result MarkAsRead(DateTimeOffset now)
    {
        if (Status == NotificationStatus.Read)
            return Result.Success(); // Idempotent

        Status = NotificationStatus.Read;
        ReadAtUtc = now;

        AddDomainEvent(new NotificationReadDomainEvent(Id, UserId));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset now)
    {
        Status = NotificationStatus.Archived;
        return Result.Success();
    }
}
