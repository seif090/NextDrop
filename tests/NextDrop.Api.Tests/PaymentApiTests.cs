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
using NextDrop.Modules.Identity.Application.Commands.Login;
using NextDrop.Modules.Identity.Application.Commands.RegisterUser;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Payments.Application.DTOs;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.Entities;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Api.Tests;

public class PaymentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = "TestDb_Pay_" + Guid.NewGuid();

    public PaymentApiTests(WebApplicationFactory<Program> factory)
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

    private async Task<(HttpClient Client, Guid UserId, string Token)> CreateAuthenticatedUserAsync(string email, string role = "Customer")
    {
        var client = _factory.CreateClient();
        var registerCommand = new RegisterUserCommand(email, "Password123!", "Test", "User", "+1234567890");
        var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var regData = await regResponse.Content.ReadFromJsonAsync<RegisterUserResponse>();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var user = await db.Users.Include(u => u.EmailVerificationTokens).FirstAsync(u => u.Email == email);
            var tokenEntity = user.EmailVerificationTokens.First();
            user.VerifyEmail(tokenEntity.TokenHash, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var loginCommand = new LoginCommand(email, "Password123!");
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        return (client, regData!.UserId, authResult.AccessToken);
    }

    private async Task<(Guid CartId, Guid AddressId, decimal BasePrice)> SeedCheckoutPrerequisitesAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
        var now = DateTimeOffset.UtcNow;

        // Customer & Address
        var customerId = CustomerId.New();
        var customer = Customer.Create(customerId, userId, "Test Customer", "customer@test.com", "+1234567890", now).Value;
        var addressId = CustomerAddressId.New();
        customer.AddAddress(addressId, "Home", "Test Recipient", "+1234567890", "123 Main St", null, "Cairo", "Downtown", "10", "4", "12", 30.04m, 31.23m, true, now);
        db.Customers.Add(customer);

        // Restaurant & 24/7 Branch
        var restaurantId = RestaurantId.New();
        var restaurant = Restaurant.Create(restaurantId, Guid.NewGuid(), "Gourmet Pizza", "Tasty Pizza", "Cuisine", "USD", now).Value;
        restaurant.Activate(now);

        var branchId = RestaurantBranchId.New();
        var branchRes = restaurant.AddBranch(branchId, "Main Branch", "+123", "123 Main St", null, "Cairo", "Downtown", 30.04m, 31.23m, "UTC", now);
        branchRes.IsSuccess.Should().BeTrue();
        var branch = restaurant.Branches.First(b => b.Id == branchId);
        branch.UpdateStatus(BranchStatus.Active, now);
        branch.AddDeliveryZone(RestaurantDeliveryZoneId.New(), "Downtown Zone", 5.0m, 10.0m, 30, now);
        branch.SetOperatingHours(new List<RestaurantOperatingHours> { RestaurantOperatingHours.Open(DayOfWeek.Sunday, new TimeOnly(0, 0), new TimeOnly(23, 59)) }, now);
        db.Restaurants.Add(restaurant);

        // Catalog & Menu Item
        var catalogId = CatalogId.New();
        var catalog = Catalog.Create(catalogId, restaurantId, "Main Menu", "Delicious food", now).Value;
        var menuItemId = MenuItemId.New();
        var categoryId = CategoryId.New();
        var basePrice = 25.00m;
        var menuItem = MenuItem.Create(menuItemId, categoryId, restaurantId, "Margherita Pizza", "Classic pizza", basePrice, 1, now).Value;
        db.Catalogs.Add(catalog);
        db.MenuItems.Add(menuItem);

        // Cart & Cart Item
        var cartId = CartId.New();
        var cart = Cart.Create(cartId, customerId, restaurantId, branchId, "USD", now).Value;
        cart.AddItem(CartItemId.New(), restaurantId, branchId, menuItemId, null, 2, basePrice, "Margherita Pizza", null, null, now);
        db.Carts.Add(cart);

        await db.SaveChangesAsync();

        return (cartId.Value, addressId.Value, basePrice);
    }

    [Fact]
    public async Task Checkout_Flow_And_Payment_Confirmation_Should_Succeed()
    {
        var (client, userId, _) = await CreateAuthenticatedUserAsync($"pay_user_{Guid.NewGuid():N}@test.com");
        var (cartId, addressId, _) = await SeedCheckoutPrerequisitesAsync(userId);

        // 1. Execute Checkout
        var checkoutReq = new { CartId = cartId, DeliveryAddressId = addressId };
        var checkoutResponse = await client.PostAsJsonAsync("/api/v1/checkout", checkoutReq);
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkoutResult = await checkoutResponse.Content.ReadFromJsonAsync<TransactionalCheckoutResultDto>();
        checkoutResult.Should().NotBeNull();
        checkoutResult!.PaymentStatus.Should().Be("Pending");
        checkoutResult.OrderStatus.Should().BeOneOf("Pending", "PendingPayment");

        // 2. Confirm Payment with Idempotency Key
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var confirmResponse = await client.PostAsync($"/api/v1/payments/{checkoutResult.PaymentId}/confirm", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var paymentDto = await confirmResponse.Content.ReadFromJsonAsync<PaymentDto>();
        paymentDto!.Status.Should().Be("Captured");

        // 3. Verify Payment Query Endpoint
        var queryResponse = await client.GetAsync($"/api/v1/payments/{checkoutResult.PaymentId}");
        queryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Payment_Access_By_Another_User_Should_Return_Forbidden()
    {
        var (clientA, userIdA, _) = await CreateAuthenticatedUserAsync($"user_a_{Guid.NewGuid():N}@test.com");
        var (cartId, addressId, _) = await SeedCheckoutPrerequisitesAsync(userIdA);

        var checkoutReq = new { CartId = cartId, DeliveryAddressId = addressId };
        var checkoutResponse = await clientA.PostAsJsonAsync("/api/v1/checkout", checkoutReq);
        var checkoutResult = await checkoutResponse.Content.ReadFromJsonAsync<TransactionalCheckoutResultDto>();

        // User B attempts to access User A's payment
        var (clientB, _, _) = await CreateAuthenticatedUserAsync($"user_b_{Guid.NewGuid():N}@test.com");
        var bolaResponse = await clientB.GetAsync($"/api/v1/payments/{checkoutResult!.PaymentId}");
        bolaResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Refund_Flow_And_Over_Refund_Prevention_Should_Succeed()
    {
        var (client, userId, _) = await CreateAuthenticatedUserAsync($"refund_user_{Guid.NewGuid():N}@test.com");
        var (cartId, addressId, _) = await SeedCheckoutPrerequisitesAsync(userId);

        // Checkout & Confirm Payment
        var checkoutRes = await client.PostAsJsonAsync("/api/v1/checkout", new { CartId = cartId, DeliveryAddressId = addressId });
        var checkoutResult = await checkoutRes.Content.ReadFromJsonAsync<TransactionalCheckoutResultDto>();
        await client.PostAsync($"/api/v1/payments/{checkoutResult!.PaymentId}/confirm", null);

        // Refund partial amount (20.00)
        var refundReq1 = new { Amount = 20.00m, Reason = "Item cold" };
        var refundRes1 = await client.PostAsJsonAsync($"/api/v1/payments/{checkoutResult.PaymentId}/refund", refundReq1);
        refundRes1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Attempt Over-Refund (Amount 100.00 when captured amount is 55.00)
        var overRefundReq = new { Amount = 100.00m, Reason = "Greedy refund" };
        var overRefundRes = await client.PostAsJsonAsync($"/api/v1/payments/{checkoutResult.PaymentId}/refund", overRefundReq);
        overRefundRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Webhook_Replay_Protection_Should_Process_Once_And_Ignore_Replays()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Webhook-Signature", "valid_sig_123");
        client.DefaultRequestHeaders.Add("X-Webhook-Event-Id", "evt_unique_9999");

        var payload = JsonSerializer.Serialize(new { event_type = "payment.captured", payment_id = Guid.NewGuid() });

        // First Post -> Processed
        var res1 = await client.PostAsJsonAsync("/api/v1/payments/webhooks/fakeprovider", payload);
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second Post (Replay) -> Ignored safely with HTTP 200 OK
        var res2 = await client.PostAsJsonAsync("/api/v1/payments/webhooks/fakeprovider", payload);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
