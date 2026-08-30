using NextDrop.Modules.Notifications.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Domain.Aggregates;

public class UserNotificationPreference : AggregateRoot<UserNotificationPreferenceId>
{
    public Guid UserId { get; private set; }
    public bool AllowOrderNotifications { get; private set; }
    public bool AllowMarketingNotifications { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool InAppEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private UserNotificationPreference() { } // EF Core

    private UserNotificationPreference(
        UserNotificationPreferenceId id,
        Guid userId,
        bool allowOrderNotifications,
        bool allowMarketingNotifications,
        bool emailEnabled,
        bool inAppEnabled,
        DateTimeOffset now)
        : base(id)
    {
        UserId = userId;
        AllowOrderNotifications = allowOrderNotifications;
        AllowMarketingNotifications = allowMarketingNotifications;
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static UserNotificationPreference CreateDefault(Guid userId, DateTimeOffset now)
    {
        return new UserNotificationPreference(
            UserNotificationPreferenceId.New(),
            userId,
            allowOrderNotifications: true, // Order notifications default enabled
            allowMarketingNotifications: false, // Marketing requires explicit opt-in
            emailEnabled: true,
            inAppEnabled: true,
            now);
    }

    public Result Update(
        bool allowOrderNotifications,
        bool allowMarketingNotifications,
        bool emailEnabled,
        bool inAppEnabled,
        DateTimeOffset now)
    {
        // Order-critical notifications MUST NOT be disabled if system policy requires them,
        // but user preferences can toggle channels.
        AllowOrderNotifications = allowOrderNotifications;
        AllowMarketingNotifications = allowMarketingNotifications;
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
        UpdatedAtUtc = now;

        return Result.Success();
    }
}
