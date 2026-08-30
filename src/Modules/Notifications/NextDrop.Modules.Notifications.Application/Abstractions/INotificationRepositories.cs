using NextDrop.Modules.Notifications.Domain.Aggregates;
using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.Modules.Notifications.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);
    Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    void Remove(Notification notification);
}

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetActiveTemplateAsync(NotificationType type, NotificationChannel channel, string language, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken = default);
}

public interface IUserNotificationPreferenceRepository
{
    Task<UserNotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default);
}

public interface IProcessedIntegrationEventRepository
{
    Task<bool> IsEventProcessedAsync(string consumerName, string eventId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedIntegrationEvent processedEvent, CancellationToken cancellationToken = default);
}

public interface INotificationChannel
{
    NotificationChannel Channel { get; }
    Task<Result<string?>> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}

public interface INotificationTemplateRenderer
{
    (string Title, string Body) Render(NotificationTemplate template, IDictionary<string, string> variables);
}

public interface IRealTimeNotificationPublisher
{
    Task PublishOrderNotificationAsync(Guid userId, Guid orderId, string eventName, object payload, CancellationToken cancellationToken = default);
    Task PublishRiderLocationAsync(Guid orderId, double latitude, double longitude, CancellationToken cancellationToken = default);
}
