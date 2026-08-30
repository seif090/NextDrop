using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.Modules.Notifications.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Domain.Entities;

public class NotificationDelivery : Entity<NotificationDeliveryId>
{
    public NotificationId NotificationId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public DateTimeOffset? FailedAtUtc { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? NextRetryAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private NotificationDelivery() { } // EF Core

    public NotificationDelivery(
        NotificationDeliveryId id,
        NotificationId notificationId,
        NotificationChannel channel,
        DateTimeOffset now)
        : base(id)
    {
        NotificationId = notificationId;
        Channel = channel;
        Status = DeliveryStatus.Pending;
        AttemptCount = 0;
        CreatedAtUtc = now;
    }

    public void MarkProcessing(DateTimeOffset now)
    {
        Status = DeliveryStatus.Processing;
        AttemptCount++;
        LastAttemptAtUtc = now;
    }

    public void MarkDelivered(string? providerMessageId, DateTimeOffset now)
    {
        Status = DeliveryStatus.Delivered;
        ProviderMessageId = providerMessageId;
        DeliveredAtUtc = now;
        LastError = null;
        NextRetryAtUtc = null;
    }

    public void RecordFailedAttempt(string error, int maxAttempts, DateTimeOffset now)
    {
        LastError = error;
        FailedAtUtc = now;

        if (AttemptCount >= maxAttempts)
        {
            Status = DeliveryStatus.DeadLettered;
            NextRetryAtUtc = null;
        }
        else
        {
            Status = DeliveryStatus.Failed;
            // Exponential backoff: 2^attempt * 1 minute
            var backoffMinutes = Math.Pow(2, AttemptCount);
            NextRetryAtUtc = now.AddMinutes(backoffMinutes);
        }
    }
}
