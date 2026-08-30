using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NextDrop.Api.Controllers.v1;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Identity.Application.Commands.Login;
using NextDrop.Modules.Identity.Application.Commands.RegisterUser;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.Modules.Notifications.Application.Commands;
using NextDrop.Modules.Notifications.Application.DTOs;
using NextDrop.Modules.Notifications.Domain.Aggregates;
using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.Modules.Notifications.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Api.Tests;

public class NotificationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = "TestDb_Notif_" + Guid.NewGuid();

    public NotificationApiTests(WebApplicationFactory<Program> factory)
    {
        var dbRoot = new InMemoryDatabaseRoot();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<NextDropDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.Name.Contains("DbContextOptions")).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<NextDropDbContext>(options =>
                {
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning));
                    options.UseInMemoryDatabase(_dbName, dbRoot);
                });
            });
        });
    }

    private async Task<(HttpClient Client, string Token, NextDrop.Modules.Identity.Domain.Aggregates.User.User User)> CreateAuthenticatedUserAsync(string emailPrefix)
    {
        var client = _factory.CreateClient();
        var email = $"{emailPrefix}_{Guid.NewGuid():N}@example.com";
        var registerCommand = new RegisterUserCommand(email, "StrongP@ss123!", "Customer", "User", "+1234567890");

        var registerResp = await client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);
        if (!registerResp.IsSuccessStatusCode)
        {
            var err = await registerResp.Content.ReadAsStringAsync();
            throw new Exception($"Register failed: {registerResp.StatusCode} - {err}");
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var user = db.Users.First(u => u.Email == email);
            var tokenEntity = db.EmailVerificationTokens.First(t => t.UserId == user.Id);
            user.VerifyEmail(tokenEntity.TokenHash, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "StrongP@ss123!" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginContent = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginContent!.AccessToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var user = db.Users.First(u => u.Email == email);
            return (client, loginContent.AccessToken, user);
        }
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnPagedNotifications()
    {
        // Arrange
        var (client, _, user) = await CreateAuthenticatedUserAsync("notif_user_paged");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var notif = Notification.Create(
                NotificationId.New(),
                user.Id.Value,
                NotificationType.OrderPlaced,
                "Test Order",
                "Your order has been received.",
                null,
                NotificationChannel.InApp,
                NotificationPriority.Normal,
                DateTimeOffset.UtcNow).Value;
            db.Notifications.Add(notif);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/api/v1/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedNotificationResultDto>();
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle(n => n.Title == "Test Order");
    }

    [Fact]
    public async Task GetUnreadNotifications_ShouldReturnUnreadOnly()
    {
        // Arrange
        var (client, _, user) = await CreateAuthenticatedUserAsync("notif_user_unread");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var unreadNotif = Notification.Create(
                NotificationId.New(),
                user.Id.Value,
                NotificationType.OrderPreparing,
                "Preparing Food",
                "Kitchen has started preparing.",
                null,
                NotificationChannel.InApp,
                NotificationPriority.Normal,
                DateTimeOffset.UtcNow).Value;

            var readNotif = Notification.Create(
                NotificationId.New(),
                user.Id.Value,
                NotificationType.OrderPlaced,
                "Order Placed",
                "Received.",
                null,
                NotificationChannel.InApp,
                NotificationPriority.Normal,
                DateTimeOffset.UtcNow).Value;
            readNotif.MarkAsRead(DateTimeOffset.UtcNow);

            db.Notifications.AddRange(unreadNotif, readNotif);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/api/v1/notifications/unread");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        items.Should().NotBeNull();
        items.Should().ContainSingle(n => n.Title == "Preparing Food");
    }

    [Fact]
    public async Task MarkAsRead_ShouldUpdateNotificationStatus()
    {
        // Arrange
        var (client, _, user) = await CreateAuthenticatedUserAsync("notif_user_read");
        Guid notifId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var notif = Notification.Create(
                NotificationId.New(),
                user.Id.Value,
                NotificationType.RiderAssigned,
                "Rider Assigned",
                "Rider John is on his way.",
                null,
                NotificationChannel.InApp,
                NotificationPriority.High,
                DateTimeOffset.UtcNow).Value;
            db.Notifications.Add(notif);
            await db.SaveChangesAsync();
            notifId = notif.Id.Value;
        }

        // Act
        var response = await client.PostAsync($"/api/v1/notifications/{notifId}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var notif = db.Notifications.First(n => n.Id == new NotificationId(notifId));
            notif.Status.Should().Be(NotificationStatus.Read);
        }
    }

    [Fact]
    public async Task BOLA_UserA_AttemptingToMarkUserBNotificationRead_ShouldReturnForbiddenOrNotFound()
    {
        // Arrange
        var (clientA, _, _) = await CreateAuthenticatedUserAsync("user_a_bola");
        var (_, _, userB) = await CreateAuthenticatedUserAsync("user_b_bola");
        Guid userBNotifId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var notif = Notification.Create(
                NotificationId.New(),
                userB.Id.Value,
                NotificationType.OrderDelivered,
                "Private User B Notification",
                "Delivered.",
                null,
                NotificationChannel.InApp,
                NotificationPriority.High,
                DateTimeOffset.UtcNow).Value;
            db.Notifications.Add(notif);
            await db.SaveChangesAsync();
            userBNotifId = notif.Id.Value;
        }

        // Act
        var response = await clientA.PostAsync($"/api/v1/notifications/{userBNotifId}/read", null);

        // Assert: BOLA Protection prevents cross-tenant access!
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Preferences_GetAndUpdate_ShouldPersistPreferences()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedUserAsync("pref_user");

        // Act 1: Get defaults
        var getResp = await client.GetAsync("/api/v1/notifications/preferences");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var getPref = await getResp.Content.ReadFromJsonAsync<UserNotificationPreferenceDto>();
        getPref!.AllowOrderNotifications.Should().BeTrue();
        getPref.AllowMarketingNotifications.Should().BeFalse();

        // Act 2: Update preferences
        var updateReq = new NotificationsController.UpdatePreferencesRequest(
            AllowOrderNotifications: true,
            AllowMarketingNotifications: true,
            EmailEnabled: false,
            InAppEnabled: true);

        var putResp = await client.PutAsJsonAsync("/api/v1/notifications/preferences", updateReq);

        // Assert 2
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPref = await putResp.Content.ReadFromJsonAsync<UserNotificationPreferenceDto>();
        updatedPref!.AllowMarketingNotifications.Should().BeTrue();
        updatedPref.EmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessIntegrationEvent_DuplicateEvent_ShouldBeDeduplicated()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();

        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var eventId = $"evt_dedup_{Guid.NewGuid():N}";

        var cmd = new ProcessIntegrationEventNotificationCommand(
            ConsumerName: "OrderNotificationsConsumer",
            EventId: eventId,
            UserId: userId,
            OrderId: orderId,
            Type: NotificationType.OrderConfirmed,
            Title: "Order Confirmed",
            Body: "Restaurant accepted your order.",
            DataJson: null);

        // Act 1: First delivery
        var result1 = await mediator.Send(cmd);

        // Act 2: Duplicate delivery of exact same integration event
        var result2 = await mediator.Send(cmd);

        // Assert: Both succeed, but inbox deduplication creates ONLY 1 notification in DB!
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var notifCount = db.Notifications.Count(n => n.UserId == userId);
        notifCount.Should().Be(1);
    }
}
