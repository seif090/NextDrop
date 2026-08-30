using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Notifications.Application.Abstractions;
using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.SharedKernel.Abstractions;

namespace NextDrop.Modules.Notifications.Infrastructure.Jobs;

public class NotificationDeliveryProcessorJob
{
    private readonly NextDropDbContext _dbContext;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<NotificationDeliveryProcessorJob> _logger;

    public NotificationDeliveryProcessorJob(
        NextDropDbContext dbContext,
        IEnumerable<INotificationChannel> channels,
        IDateTimeProvider dateTimeProvider,
        ILogger<NotificationDeliveryProcessorJob> logger)
    {
        _dbContext = dbContext;
        _channels = channels;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var pendingNotifications = await _dbContext.Notifications
            .Include(n => n.Deliveries)
            .Where(n => n.Deliveries.Any(d => d.Status == DeliveryStatus.Pending || (d.Status == DeliveryStatus.Failed && d.NextRetryAtUtc <= now)))
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var notification in pendingNotifications)
        {
            foreach (var delivery in notification.Deliveries.Where(d => d.Status == DeliveryStatus.Pending || (d.Status == DeliveryStatus.Failed && d.NextRetryAtUtc <= now)))
            {
                var channelHandler = _channels.FirstOrDefault(c => c.Channel == delivery.Channel);
                if (channelHandler == null)
                {
                    delivery.RecordFailedAttempt($"No channel handler registered for {delivery.Channel}", 3, now);
                    continue;
                }

                delivery.MarkProcessing(now);
                try
                {
                    var result = await channelHandler.SendAsync(notification, cancellationToken);
                    if (result.IsSuccess)
                    {
                        delivery.MarkDelivered(result.Value, now);
                    }
                    else
                    {
                        delivery.RecordFailedAttempt(result.Error.Description, 3, now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error delivering notification {NotificationId} via channel {Channel}", notification.Id.Value, delivery.Channel);
                    delivery.RecordFailedAttempt(ex.Message, 3, now);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
