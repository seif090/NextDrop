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
using NextDrop.Infrastructure.Persistence.Interceptors;
using NextDrop.Modules.Catalog.Application.DTOs;
using NextDrop.Modules.Identity.Application.Commands.Login;
using NextDrop.Modules.Identity.Application.Commands.RegisterUser;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.Modules.Restaurants.Application.DTOs;
using Xunit;

namespace NextDrop.Api.Tests;

public class CatalogApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = "TestDb_Cat_" + Guid.NewGuid();

    public CatalogApiTests(WebApplicationFactory<Program> factory)
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
    public async Task CompleteCatalogLifecycle_Create_Category_Item_Price_Publish_ReadPublic_ShouldSucceed()
    {
        // Arrange
        var (ownerClient, _, _) = await RegisterAndLoginUserAsync("catowner");

        // 1. Create Restaurant
        var restResp = await ownerClient.PostAsJsonAsync("/api/v1/restaurants", new CreateRestaurantRequest("Gourmet Burger", "Best burgers", "+123", "b@gourmet.com"));
        var rest = await restResp.Content.ReadFromJsonAsync<RestaurantDto>();

        // 2. Create Catalog (Draft)
        var catResp = await ownerClient.PostAsJsonAsync($"/api/v1/restaurants/{rest!.Id}/catalog", new CreateCatalogRequest("Main Menu", "Delicious options"));
        catResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var catalog = await catResp.Content.ReadFromJsonAsync<CatalogDto>();
        catalog!.Status.Should().Be("Draft");

        // 3. Draft catalog is NOT visible on Public API (Scenario 6)
        var anonClient = _factory.CreateClient();
        var publicDraftResp = await anonClient.GetAsync($"/api/v1/restaurants/{rest.Id}/catalog");
        publicDraftResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 4. Add Category
        var categoryResp = await ownerClient.PostAsJsonAsync($"/api/v1/catalogs/{catalog.Id}/categories", new CreateCategoryRequest("Burgers", "Juicy burgers", 0));
        categoryResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Fetch catalog to get CategoryId
        var fetchedCatalogResp = await ownerClient.GetAsync($"/api/v1/catalogs/{catalog.Id}");
        var fetchedCatalog = await fetchedCatalogResp.Content.ReadFromJsonAsync<CatalogDto>();
        var categoryId = fetchedCatalog!.Categories.First().Id;

        // 5. Add Menu Item
        var itemResp = await ownerClient.PostAsJsonAsync($"/api/v1/categories/{categoryId}/items", new CreateMenuItemRequest("Smokey Burger", "Bacon & Cheese", 120.00m, 0));
        itemResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await itemResp.Content.ReadFromJsonAsync<MenuItemDto>();

        // 6. Change Price
        var priceResp = await ownerClient.PutAsJsonAsync($"/api/v1/menu-items/{item!.Id}/price", new ChangePriceRequest(135.00m));
        priceResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. Publish Catalog
        var publishResp = await ownerClient.PostAsJsonAsync($"/api/v1/catalogs/{catalog.Id}/publish", new { });
        publishResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishedCatalog = await publishResp.Content.ReadFromJsonAsync<CatalogDto>();
        publishedCatalog!.Status.Should().Be("Published");

        // 8. Read Public Catalog (Consumers)
        var publicResp = await anonClient.GetAsync($"/api/v1/restaurants/{rest.Id}/catalog");
        publicResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicCatalog = await publicResp.Content.ReadFromJsonAsync<PublicCatalogDto>();
        publicCatalog!.Categories.Should().HaveCount(1);
        publicCatalog.Categories.First().MenuItems.First().BasePrice.Should().Be(135.00m);
    }

    [Fact]
    public async Task SecurityScenario2_OwnerA_ModifyingOwnerBRestaurantCatalog_ShouldReturn403Forbidden()
    {
        // Arrange
        var (ownerAClient, _, _) = await RegisterAndLoginUserAsync("catownera");
        var (ownerBClient, _, _) = await RegisterAndLoginUserAsync("catownerb");

        // Owner B creates restaurant & catalog
        var restBResp = await ownerBClient.PostAsJsonAsync("/api/v1/restaurants", new CreateRestaurantRequest("B's Diner", "Desc", "+1", "b@diner.com"));
        var restB = await restBResp.Content.ReadFromJsonAsync<RestaurantDto>();

        // Act: Owner A attempts to create catalog for Owner B's restaurant
        var hackResp = await ownerAClient.PostAsJsonAsync($"/api/v1/restaurants/{restB!.Id}/catalog", new CreateCatalogRequest("Hacked Menu", "Desc"));

        // Assert: 403 Forbidden
        hackResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
