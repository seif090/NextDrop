using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Domain.ValueObjects;

public readonly record struct NotificationId(Guid Value)
{
    public static NotificationId New() => new(Guid.NewGuid());
    public static NotificationId Empty => new(Guid.Empty);
}

public readonly record struct NotificationTemplateId(Guid Value)
{
    public static NotificationTemplateId New() => new(Guid.NewGuid());
    public static NotificationTemplateId Empty => new(Guid.Empty);
}

public readonly record struct NotificationDeliveryId(Guid Value)
{
    public static NotificationDeliveryId New() => new(Guid.NewGuid());
    public static NotificationDeliveryId Empty => new(Guid.Empty);
}

public readonly record struct UserNotificationPreferenceId(Guid Value)
{
    public static UserNotificationPreferenceId New() => new(Guid.NewGuid());
    public static UserNotificationPreferenceId Empty => new(Guid.Empty);
}
