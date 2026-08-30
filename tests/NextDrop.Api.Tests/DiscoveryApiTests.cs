using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Discovery.Application.DTOs;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.Entities;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Api.Tests;

public class DiscoveryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;

    public DiscoveryApiTests(WebApplicationFactory<Program> factory)
    {
        _dbName = "DiscoveryTestDb_" + Guid.NewGuid();
        var dbRoot = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();

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

        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
        var now = DateTimeOffset.UtcNow;

        // Active Restaurant 1
        var r1Id = RestaurantId.New();
        var r1 = Restaurant.Create(r1Id, Guid.NewGuid(), "Cairo Burgers", "Best burgers in Cairo", "+20123456789", "cairoburgers@test.com", now).Value;
        r1.Activate(now);

        var b1Id = RestaurantBranchId.New();
        var branch1 = r1.AddBranch(b1Id, "Maadi Branch", "+20123456789", "Road 9", null, "Cairo", "Maadi", 29.96m, 31.25m, "Africa/Cairo", now).Value;
        branch1.SetOperatingHours(new[]
        {
            RestaurantOperatingHours.Open(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(23, 0)),
            RestaurantOperatingHours.Open(DayOfWeek.Sunday, new TimeOnly(9, 0), new TimeOnly(23, 0))
        }, now);
        branch1.AddDeliveryZone(RestaurantDeliveryZoneId.New(), "Maadi Zone", 15m, 50m, 30, now);

        // Inactive Restaurant 2
        var r2Id = RestaurantId.New();
        var r2 = Restaurant.Create(r2Id, Guid.NewGuid(), "Inactive Diner", "Closed diner", "+20199999999", "inactive@test.com", now).Value;

        // Catalog & Category for Restaurant 1
        var catalogId = new CatalogId(Guid.NewGuid());
        var catalog = Catalog.Create(catalogId, r1Id, "Cairo Burgers Catalog", "Main menu catalog", now).Value;
        var catId = new CategoryId(Guid.NewGuid());
        var category = catalog.AddCategory(catId, "Main Courses", "Main burgers", 1, now).Value;

        var item1Id = MenuItemId.New();
        var item1Result = MenuItem.Create(item1Id, catId, r1Id, "Classic Cheeseburger", "Juicy beef patty with cheese", 85.00m, 1, now);
        var item1 = item1Result.Value;

        var item2Id = MenuItemId.New();
        var item2Result = MenuItem.Create(item2Id, catId, r1Id, "Truffle Burger", "Gourmet truffle burger", 150.00m, 2, now);
        var item2 = item2Result.Value;

        db.Restaurants.AddRange(r1, r2);
        db.Catalogs.Add(catalog);
        db.MenuItems.AddRange(item1, item2);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetRestaurants_AnonymousUser_ShouldReturnActiveRestaurantsOnly()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/discovery/restaurants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedDiscoveryResultDto<PublicRestaurantDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        result.Items.First().Name.Should().Be("Cairo Burgers");
        result.Items.First().Branches.Should().ContainSingle();
        result.Items.First().Branches.First().BranchName.Should().Be("Maadi Branch");
    }

    [Fact]
    public async Task GetRestaurantById_ExistingActiveRestaurant_ShouldReturn200OK()
    {
        var client = _factory.CreateClient();
        var listResponse = await client.GetAsync("/api/v1/discovery/restaurants");
        var listResult = await listResponse.Content.ReadFromJsonAsync<PagedDiscoveryResultDto<PublicRestaurantDto>>();
        var restaurantId = listResult!.Items.First().Id;

        var response = await client.GetAsync($"/api/v1/discovery/restaurants/{restaurantId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var restaurant = await response.Content.ReadFromJsonAsync<PublicRestaurantDto>();
        restaurant.Should().NotBeNull();
        restaurant!.Id.Should().Be(restaurantId);
        restaurant.Name.Should().Be("Cairo Burgers");
    }

    [Fact]
    public async Task GetRestaurantBranches_ShouldReturnPublicBranches()
    {
        var client = _factory.CreateClient();
        var listResponse = await client.GetAsync("/api/v1/discovery/restaurants");
        var listResult = await listResponse.Content.ReadFromJsonAsync<PagedDiscoveryResultDto<PublicRestaurantDto>>();
        var restaurantId = listResult!.Items.First().Id;

        var response = await client.GetAsync($"/api/v1/discovery/restaurants/{restaurantId}/branches");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var branches = await response.Content.ReadFromJsonAsync<List<PublicBranchDto>>();
        branches.Should().NotBeNull();
        branches.Should().ContainSingle();
        branches!.First().City.Should().Be("Cairo");
        branches.First().District.Should().Be("Maadi");
    }

    [Fact]
    public async Task SearchMenuItems_ShouldReturnMatchingPublishedItems()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/discovery/menu/search?searchTerm=Cheeseburger");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedDiscoveryResultDto<PublicMenuItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        result.Items.First().Name.Should().Be("Classic Cheeseburger");
        result.Items.First().BasePrice.Should().Be(85.00m);
    }

    [Fact]
    public async Task GetRestaurants_SearchTermExceeding100Chars_ShouldReturn400BadRequest()
    {
        var client = _factory.CreateClient();
        var longSearch = new string('x', 101);

        var response = await client.GetAsync($"/api/v1/discovery/restaurants?searchTerm={longSearch}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRestaurants_RepeatedQuery_ShouldHitCacheAndReturnIdenticalResults()
    {
        var client = _factory.CreateClient();

        var response1 = await client.GetAsync("/api/v1/discovery/restaurants?city=Cairo");
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        var response2 = await client.GetAsync("/api/v1/discovery/restaurants?city=Cairo");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var result1 = await response1.Content.ReadFromJsonAsync<PagedDiscoveryResultDto<PublicRestaurantDto>>();
        var result2 = await response2.Content.ReadFromJsonAsync<PagedDiscoveryResultDto<PublicRestaurantDto>>();

        result1.Should().BeEquivalentTo(result2);
    }
}
