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
using NextDrop.Modules.Orders.Domain.Entities;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Api.Tests;

public class OrdersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = "TestDb_Ord_" + Guid.NewGuid();

    public OrdersApiTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task Cart_Lifecycle_And_Checkout_Flow_Should_Work_Correctly()
    {
        var (client, _, userId) = await RegisterAndLoginUserAsync("custorder");

        var restId = RestaurantId.New();
        var branchId = RestaurantBranchId.New();
        var customerId = CustomerId.New();
        var addressId = CustomerAddressId.New();
        var menuItemId = MenuItemId.New();
        var catalogId = CatalogId.New();
        var categoryId = CategoryId.New();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var now = DateTimeOffset.UtcNow;

            // Seed Customer profile with active address
            var customer = Customer.Create(customerId, userId, "John Doe", "john@test.com", "+1234567890", now).Value;
            customer.AddAddress(addressId, "Home", "John", "+1234567890", "Main St", null, "Cairo", "Maadi", "10", "2", "5", 30.0m, 31.0m, true, now);
            db.Customers.Add(customer);

            // Seed Restaurant & Branch
            var restaurant = Restaurant.Create(restId, Guid.NewGuid(), "Pizza Palace", "Tasty pizza", "+1234567890", "pizza@palace.com", now).Value;
            restaurant.Activate(now);
            var branch = restaurant.AddBranch(branchId, "Branch 1", "+1234567890", "Main St", null, "Cairo", "Maadi", 30.0m, 31.0m, "UTC", now).Value;

            // Seed 24/7 Operating Hours for all days of week
            var hours = Enum.GetValues<DayOfWeek>().Select(day =>
                RestaurantOperatingHours.Open(day, new TimeOnly(0, 0), new TimeOnly(23, 59))
            ).ToList();
            branch.SetOperatingHours(hours, now);

            branch.AddDeliveryZone(RestaurantDeliveryZoneId.New(), "Zone 1", 20.00m, 30.00m, 30, now);
            db.Restaurants.Add(restaurant);

            // Seed Catalog & Menu Item
            var catalog = Catalog.Create(catalogId, restId, "Menu", "Desc", now).Value;
            catalog.AddCategory(categoryId, "Pizzas", null, 0, now);
            catalog.Publish(now, 1);
            db.Catalogs.Add(catalog);

            var menuItem = MenuItem.Create(menuItemId, categoryId, restId, "Pepperoni Pizza", "Classic", 50.00m, 0, now).Value;
            db.MenuItems.Add(menuItem);

            await db.SaveChangesAsync();
        }

        // 1. Create Cart
        var createCartResp = await client.PostAsJsonAsync("/api/v1/carts", new { RestaurantId = restId.Value, RestaurantBranchId = branchId.Value });
        createCartResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var cartJson = await createCartResp.Content.ReadFromJsonAsync<JsonElement>();
        var cartId = cartJson.GetProperty("id").GetGuid();

        // 2. Add Cart Item (Quantity = 2 -> 2 * 50 = 100 subtotal, satisfies min order 30)
        var addItemResp = await client.PostAsJsonAsync($"/api/v1/carts/{cartId}/items", new { MenuItemId = menuItemId.Value, Quantity = 2, Notes = "Extra cheese" });
        addItemResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Checkout Cart (Idempotent)
        var idempotencyKey = $"checkout-{Guid.NewGuid()}";
        var checkoutReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/carts/{cartId}/checkout")
        {
            Content = JsonContent.Create(new { DeliveryAddressId = addressId.Value })
        };
        checkoutReq.Headers.Add("Idempotency-Key", idempotencyKey);

        var checkoutResp = await client.SendAsync(checkoutReq);
        checkoutResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkoutJson = await checkoutResp.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = checkoutJson.GetProperty("orderId").GetGuid();
        var orderNumber = checkoutJson.GetProperty("orderNumber").GetString();
        orderNumber.Should().StartWith("ND-2026-");

        // 4. Idempotent Replay check
        var replayReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/carts/{cartId}/checkout")
        {
            Content = JsonContent.Create(new { DeliveryAddressId = addressId.Value })
        };
        replayReq.Headers.Add("Idempotency-Key", idempotencyKey);

        var replayResp = await client.SendAsync(replayReq);
        replayResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Get Order Details
        var getOrderResp = await client.GetAsync($"/api/v1/orders/{orderId}");
        getOrderResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Customer_Accessing_Another_Customer_Order_Should_Be_Forbidden()
    {
        var (clientA, _, userAId) = await RegisterAndLoginUserAsync("custA");
        var (clientB, _, userBId) = await RegisterAndLoginUserAsync("custB");

        var orderId = OrderId.New();
        var custAId = CustomerId.New();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var now = DateTimeOffset.UtcNow;

            var customerA = Customer.Create(custAId, userAId, "Cust A", "a@test.com", "+123", now).Value;
            db.Customers.Add(customerA);

            var cartItem = Cart.Create(CartId.New(), custAId, RestaurantId.New(), RestaurantBranchId.New(), "USD", now).Value
                .AddItem(CartItemId.New(), RestaurantId.New(), RestaurantBranchId.New(), MenuItemId.New(), null, 1, 20.00m, "Pizza", null, null, now).Value;

            var addressSnapshot = new OrderDeliveryAddress("A", "+123", "Line 1", null, "Cairo", "Maadi", "1", "2", "3", 30.0m, 31.0m);
            var order = Order.Create(orderId, "ND-2026-BOLA001", custAId, RestaurantId.New(), RestaurantBranchId.New(), addressSnapshot, "USD", 10.00m, 0.00m, new List<(CartItem CartItem, decimal ServerPrice)> { (cartItem, 20.00m) }, now).Value;
            db.Orders.Add(order);

            await db.SaveChangesAsync();
        }

        // Customer B tries to read Customer A's order -> 403 Forbidden
        var resp = await clientB.GetAsync($"/api/v1/orders/{orderId.Value}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
