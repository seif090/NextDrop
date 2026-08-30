using Microsoft.Extensions.Logging;
using NextDrop.Modules.Notifications.Application.Abstractions;
using NextDrop.Modules.Notifications.Domain.Aggregates;
using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Infrastructure.Services;

public class InAppNotificationChannel : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public Task<Result<string?>> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // In-app notifications are stored directly in DB for UI retrieval
        return Task.FromResult(Result.Success<string?>($"in_app_{notification.Id.Value:N}"));
    }
}

public class DevEmailNotificationChannel : INotificationChannel
{
    private readonly ILogger<DevEmailNotificationChannel> _logger;

    public DevEmailNotificationChannel(ILogger<DevEmailNotificationChannel> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public Task<Result<string?>> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DevEmail] Dispatching Email Notification ID {NotificationId} to UserId {UserId} | Subject: {Title}",
            notification.Id.Value,
            notification.UserId,
            notification.Title);

        return Task.FromResult(Result.Success<string?>($"dev_email_{Guid.NewGuid():N}"));
    }
}
