using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Infrastructure.Persistence.Interceptors;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Customers.Domain.Aggregates;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Delivery.Application.DTOs;
using NextDrop.Modules.Delivery.Domain.Aggregates;
using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.Modules.Identity.Application.Commands.Login;
using NextDrop.Modules.Identity.Application.Commands.RegisterUser;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Orders.Domain.Entities;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Api.Tests;

public class DeliveryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = "TestDb_Del_" + Guid.NewGuid();

    public DeliveryApiTests(WebApplicationFactory<Program> factory)
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

                services.AddDbContext<NextDropDbContext>((sp, options) =>
                {
                    var interceptor = sp.GetService<DomainEventsToOutboxInterceptor>();
                    var opts = options.UseInMemoryDatabase(_dbName, dbRoot);
                    if (interceptor != null)
                    {
                        opts.AddInterceptors(interceptor);
                    }
                });
            });
        });
    }

    private async Task<(HttpClient Client, string Token, Guid UserId)> RegisterAndLoginUserAsync(string emailPrefix)
    {
        var client = _factory.CreateClient();
        var email = $"{emailPrefix}_{Guid.NewGuid():N}@nextdrop.com".ToLowerInvariant();

        var regCommand = new RegisterUserCommand(email, "SecurePwd123!", "Test", "User", "+1234567890");
        var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", regCommand);
        var regData = await regResponse.Content.ReadFromJsonAsync<RegisterUserResponse>();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var user = await db.Users.Include(u => u.EmailVerificationTokens).FirstAsync(u => u.Email == email);
            var tokenEntity = user.EmailVerificationTokens.First();
            user.VerifyEmail(tokenEntity.TokenHash, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "SecurePwd123!"));
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData!.AccessToken);
        return (client, authData.AccessToken, regData!.UserId);
    }

    private async Task<RiderDto> CreateAndActivateRiderAsync(HttpClient client, Guid userId, string firstName)
    {
        var vehicleDto = new VehicleDto("Motorcycle", "ABC-123", "Red Honda");
        var createRiderResp = await client.PostAsJsonAsync("/api/v1/riders", new { FirstName = firstName, LastName = "Speed", PhoneNumber = "+1234567890", Vehicle = vehicleDto });
        createRiderResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var rider = await createRiderResp.Content.ReadFromJsonAsync<RiderDto>();

        // Activate rider
        var actResp = await client.PostAsync($"/api/v1/riders/{rider!.Id}/activate", null);
        actResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Set available
        var availResp = await client.PostAsJsonAsync("/api/v1/riders/me/availability", new { AvailabilityStatus = "Available" });
        availResp.StatusCode.Should().Be(HttpStatusCode.OK);

        return rider;
    }

    [Fact]
    public async Task EndToEnd_Delivery_Fulfillment_Lifecycle_Should_Succeed()
    {
        var (riderClient, _, riderUserId) = await RegisterAndLoginUserAsync("rider1");
        var rider = await CreateAndActivateRiderAsync(riderClient, riderUserId, "Flash");

        var (custClient, _, custUserId) = await RegisterAndLoginUserAsync("customer1");

        var restId = RestaurantId.New();
        var branchId = RestaurantBranchId.New();
        var customerId = CustomerId.New();
        var addressId = CustomerAddressId.New();
        var orderId = OrderId.New();
        var deliveryId = DeliveryId.New();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var now = DateTimeOffset.UtcNow;

            var customer = Customer.Create(customerId, custUserId, "Customer One", "cust1@test.com", "+1234567890", now).Value;
            db.Customers.Add(customer);

            var addressSnapshot = new OrderDeliveryAddress("Customer One", "+1234567890", "Main St", null, "Cairo", "Maadi", "10", "2", "5", 30.0m, 31.0m);
            var cartItem = Cart.Create(CartId.New(), customerId, restId, branchId, "USD", now).Value
                .AddItem(CartItemId.New(), restId, branchId, MenuItemId.New(), null, 1, 50.00m, "Pizza", null, null, now).Value;

            var order = Order.Create(orderId, "ND-2026-DELIV01", customerId, restId, branchId, addressSnapshot, "USD", 10.00m, 0.00m, new List<(CartItem CartItem, decimal ServerPrice)> { (cartItem, 50.00m) }, now).Value;
            db.Orders.Add(order);

            var delivery = Delivery.Create(deliveryId, orderId, branchId, customerId, now).Value;
            delivery.RequestRiderSearch(now);
            db.Deliveries.Add(delivery);

            await db.SaveChangesAsync();
        }

        // 1. Rider accepts delivery
        var acceptReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deliveries/{deliveryId.Value}/accept");
        acceptReq.Headers.Add("Idempotency-Key", $"accept-{Guid.NewGuid()}");
        var acceptResp = await riderClient.SendAsync(acceptReq);
        acceptResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Rider arrives at restaurant
        var arriveResp = await riderClient.PostAsync($"/api/v1/deliveries/{deliveryId.Value}/arrive", null);
        arriveResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3. Rider confirms pickup
        var pickupReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deliveries/{deliveryId.Value}/pickup");
        pickupReq.Headers.Add("Idempotency-Key", $"pickup-{Guid.NewGuid()}");
        var pickupResp = await riderClient.SendAsync(pickupReq);
        pickupResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Rider starts delivery
        var startResp = await riderClient.PostAsync($"/api/v1/deliveries/{deliveryId.Value}/start", null);
        startResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. Rider completes delivery
        var completeReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deliveries/{deliveryId.Value}/complete");
        completeReq.Headers.Add("Idempotency-Key", $"complete-{Guid.NewGuid()}");
        var completeResp = await riderClient.SendAsync(completeReq);
        completeResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify Delivery Status is Delivered
        var getDelResp = await riderClient.GetAsync($"/api/v1/deliveries/{deliveryId.Value}");
        getDelResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var delDto = await getDelResp.Content.ReadFromJsonAsync<DeliveryDto>();
        delDto!.Status.Should().Be("Delivered");
    }

    [Fact]
    public async Task Customer_Accessing_Another_Customer_Delivery_Should_Be_Forbidden()
    {
        var (clientA, _, userAId) = await RegisterAndLoginUserAsync("custDelA");
        var (clientB, _, userBId) = await RegisterAndLoginUserAsync("custDelB");

        var deliveryId = DeliveryId.New();
        var custAId = CustomerId.New();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var now = DateTimeOffset.UtcNow;

            var customerA = Customer.Create(custAId, userAId, "Cust A", "a@test.com", "+123", now).Value;
            db.Customers.Add(customerA);

            var delivery = Delivery.Create(deliveryId, OrderId.New(), RestaurantBranchId.New(), custAId, now).Value;
            db.Deliveries.Add(delivery);

            await db.SaveChangesAsync();
        }

        // Customer B tries to read Customer A's delivery -> 403 Forbidden
        var resp = await clientB.GetAsync($"/api/v1/deliveries/{deliveryId.Value}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
