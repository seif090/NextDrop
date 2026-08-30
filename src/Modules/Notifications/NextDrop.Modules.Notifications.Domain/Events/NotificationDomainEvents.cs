using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.Modules.Notifications.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Domain.Events;

public record NotificationCreatedDomainEvent(
    NotificationId NotificationId,
    Guid UserId,
    NotificationType Type,
    NotificationChannel Channel) : IDomainEvent;

public record NotificationReadDomainEvent(
    NotificationId NotificationId,
    Guid UserId) : IDomainEvent;
