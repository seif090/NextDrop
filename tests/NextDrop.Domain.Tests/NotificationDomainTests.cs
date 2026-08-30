using FluentAssertions;
using NextDrop.Modules.Notifications.Domain.Aggregates;
using NextDrop.Modules.Notifications.Domain.Entities;
using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.Modules.Notifications.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Domain.Tests;

public class NotificationDomainTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidParameters_ShouldSucceedAndAddInAppDelivery()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var id = NotificationId.New();

        // Act
        var result = Notification.Create(
            id,
            userId,
            NotificationType.OrderPlaced,
            "Order Placed",
            "Your order has been placed successfully.",
            "{\"orderId\":\"123\"}",
            NotificationChannel.InApp,
            NotificationPriority.High,
            _now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var notification = result.Value;
        notification.Id.Should().Be(id);
        notification.UserId.Should().Be(userId);
        notification.Status.Should().Be(NotificationStatus.Unread);
        notification.Deliveries.Should().HaveCount(1);
        notification.Deliveries.First().Channel.Should().Be(NotificationChannel.InApp);
        notification.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        // Act
        var result = Notification.Create(
            NotificationId.New(),
            Guid.Empty,
            NotificationType.OrderPlaced,
            "Title",
            "Body",
            null,
            NotificationChannel.InApp,
            NotificationPriority.Normal,
            _now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.EmptyUser");
    }

    [Fact]
    public void MarkAsRead_WhenUnread_ShouldUpdateStatusAndReadAt()
    {
        // Arrange
        var notification = Notification.Create(
            NotificationId.New(),
            Guid.NewGuid(),
            NotificationType.OrderDelivered,
            "Order Delivered",
            "Your food has arrived!",
            null,
            NotificationChannel.InApp,
            NotificationPriority.High,
            _now).Value;

        // Act
        var readResult = notification.MarkAsRead(_now);

        // Assert
        readResult.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Read);
        notification.ReadAtUtc.Should().Be(_now);
        notification.DomainEvents.Should().HaveCount(2); // Created + Read
    }

    [Fact]
    public void RecordFailedAttempt_ShouldCalculateExponentialBackoff_OrDeadLetter()
    {
        // Arrange
        var delivery = new NotificationDelivery(NotificationDeliveryId.New(), NotificationId.New(), NotificationChannel.Email, _now);

        // Act 1: First Failure
        delivery.MarkProcessing(_now);
        delivery.RecordFailedAttempt("Network timeout", maxAttempts: 3, _now);

        // Assert 1
        delivery.Status.Should().Be(DeliveryStatus.Failed);
        delivery.AttemptCount.Should().Be(1);
        delivery.NextRetryAtUtc.Should().Be(_now.AddMinutes(2)); // 2^1 = 2 mins

        // Act 2: Second Failure
        delivery.MarkProcessing(_now);
        delivery.RecordFailedAttempt("Connection reset", maxAttempts: 3, _now);
        delivery.NextRetryAtUtc.Should().Be(_now.AddMinutes(4)); // 2^2 = 4 mins

        // Act 3: Third Failure (Reached maxAttempts = 3)
        delivery.MarkProcessing(_now);
        delivery.RecordFailedAttempt("Permanent failure", maxAttempts: 3, _now);

        // Assert 3
        delivery.Status.Should().Be(DeliveryStatus.DeadLettered);
        delivery.NextRetryAtUtc.Should().BeNull();
    }

    [Fact]
    public void UserNotificationPreference_CreateDefault_ShouldEnableOrderNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var pref = UserNotificationPreference.CreateDefault(userId, _now);

        // Assert
        pref.UserId.Should().Be(userId);
        pref.AllowOrderNotifications.Should().BeTrue();
        pref.AllowMarketingNotifications.Should().BeFalse();
        pref.EmailEnabled.Should().BeTrue();
        pref.InAppEnabled.Should().BeTrue();
    }
}
