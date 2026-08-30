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
using NextDrop.Modules.Customers.Application.DTOs;
using NextDrop.Modules.Identity.Application.Commands.Login;
using NextDrop.Modules.Identity.Application.Commands.RegisterUser;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.Modules.Restaurants.Application.DTOs;
using NextDrop.Modules.Restaurants.Domain.Enums;
using Xunit;

namespace NextDrop.Api.Tests;

public class CustomerAndRestaurantApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = "TestDb_CR_" + Guid.NewGuid();

    public CustomerAndRestaurantApiTests(WebApplicationFactory<Program> factory)
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
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning));
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
        if (!regResponse.IsSuccessStatusCode)
        {
            var err = await regResponse.Content.ReadAsStringAsync();
            throw new Exception($"Register failed: {regResponse.StatusCode} - {err}");
        }
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
    public async Task CustomerFlow_CreateProfile_AddAddress_SetDefault_ShouldSucceed()
    {
        // Arrange
        var (client, _, _) = await RegisterAndLoginUserAsync("customer1");

        // Act 1: Update Profile
        var profileRequest = new CreateOrUpdateCustomerProfileRequest("John", "Doe", "+12345678");
        var profileResponse = await client.PutAsJsonAsync("/api/v1/customers/me", profileRequest);
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2: Add Address
        var addressRequest = new AddAddressRequest("Home", "John Doe", "+12345678", "123 Main St", null, "Cairo", "Maadi", "10", "2", "201", 30.0m, 31.0m, true);
        var addressResponse = await client.PostAsJsonAsync("/api/v1/customers/me/addresses", addressRequest);
        addressResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var addressDto = await addressResponse.Content.ReadFromJsonAsync<CustomerAddressDto>();

        // Act 3: Get Profile
        var getProfileResponse = await client.GetAsync("/api/v1/customers/me");
        getProfileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedProfile = await getProfileResponse.Content.ReadFromJsonAsync<CustomerDto>();
        fetchedProfile!.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task RestaurantFlow_OwnerCreateRestaurant_Branch_Hours_DeliveryZone_ShouldSucceed()
    {
        // Arrange
        var (ownerClient, _, _) = await RegisterAndLoginUserAsync("owner1");

        // Act 1: Create Restaurant
        var restRequest = new CreateRestaurantRequest("Burger King", "Home of Whopper", "+12345", "owner@burgerking.com");
        var restResponse = await ownerClient.PostAsJsonAsync("/api/v1/restaurants", restRequest);
        restResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var restaurantDto = await restResponse.Content.ReadFromJsonAsync<RestaurantDto>();

        // Act 2: Create Branch
        var branchRequest = new CreateBranchRequest("Maadi Branch", "+12345", "Road 9", null, "Cairo", "Maadi", 30.0m, 31.0m, "Africa/Cairo");
        var branchResponse = await ownerClient.PostAsJsonAsync($"/api/v1/restaurants/{restaurantDto!.Id}/branches", branchRequest);
        branchResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var branchDto = await branchResponse.Content.ReadFromJsonAsync<RestaurantBranchDto>();

        // Act 3: Set Operating Hours (including overnight hours 18:00 to 02:00)
        var hours = new List<RestaurantOperatingHoursDto>
        {
            new("Friday", "18:00", "02:00", false)
        };
        var hoursResponse = await ownerClient.PutAsJsonAsync($"/api/v1/restaurants/{restaurantDto.Id}/branches/{branchDto!.Id}/operating-hours", hours);
        hoursResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act 4: Create Delivery Zone
        var zoneRequest = new CreateDeliveryZoneRequest("Maadi Central", 25.00m, 100.00m, 30);
        var zoneResponse = await ownerClient.PostAsJsonAsync($"/api/v1/restaurants/{restaurantDto.Id}/branches/{branchDto.Id}/delivery-zones", zoneRequest);
        zoneResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // MANDATORY SECURITY SCENARIOS 1-4
    [Fact]
    public async Task SecurityScenario1_CustomerA_AccessingCustomerBAddress_ShouldBeDenied()
    {
        // Arrange
        var (clientA, _, _) = await RegisterAndLoginUserAsync("custa");
        var (clientB, _, _) = await RegisterAndLoginUserAsync("custb");

        // B adds an address
        var bAddressReq = new AddAddressRequest("Home", "B", "+1", "Line1", null, "City", "Dist", null, null, null, 0, 0, true);
        var bAddressResp = await clientB.PostAsJsonAsync("/api/v1/customers/me/addresses", bAddressReq);
        var bAddress = await bAddressResp.Content.ReadFromJsonAsync<CustomerAddressDto>();

        // Act: Customer A attempts to deactivate Customer B's address
        var deactivateResp = await clientA.DeleteAsync($"/api/v1/customers/me/addresses/{bAddress!.Id}");

        // Assert: 404 Not Found (Resource hiding)
        deactivateResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SecurityScenario2_OwnerA_ModifyingOwnerBRestaurant_ShouldReturn403Forbidden()
    {
        // Arrange
        var (ownerAClient, _, _) = await RegisterAndLoginUserAsync("ownera");
        var (ownerBClient, _, _) = await RegisterAndLoginUserAsync("ownerb");

        // Owner B creates a restaurant
        var createResp = await ownerBClient.PostAsJsonAsync("/api/v1/restaurants", new CreateRestaurantRequest("B's Cafe", "Desc", "+1", "b@cafe.com"));
        var restB = await createResp.Content.ReadFromJsonAsync<RestaurantDto>();

        // Act: Owner A attempts to update Owner B's restaurant
        var updateResp = await ownerAClient.PutAsJsonAsync($"/api/v1/restaurants/{restB!.Id}", new CreateRestaurantRequest("Hacked Cafe", "Desc", "+1", "b@cafe.com"));

        // Assert: 403 Forbidden
        updateResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SecurityScenario3_RestaurantStaff_AttemptingOwnerOnlyOperation_ShouldReturn403Forbidden()
    {
        // Arrange
        var (ownerClient, _, _) = await RegisterAndLoginUserAsync("restowner");
        var (staffClient, _, staffUserId) = await RegisterAndLoginUserAsync("reststaff");

        var restResp = await ownerClient.PostAsJsonAsync("/api/v1/restaurants", new CreateRestaurantRequest("Staff Test Rest", "Desc", "+1", "st@rest.com"));
        var rest = await restResp.Content.ReadFromJsonAsync<RestaurantDto>();

        // Owner adds staff user as Staff role (not Owner)
        await ownerClient.PostAsJsonAsync($"/api/v1/restaurants/{rest!.Id}/staff", new AddStaffRequest(staffUserId, RestaurantStaffRole.Staff));

        // Act: Staff attempts Owner-only action (updating restaurant status)
        var statusResp = await staffClient.PutAsJsonAsync($"/api/v1/restaurants/{rest.Id}/status", new UpdateStatusRequest(RestaurantStatus.Active));

        // Assert: 403 Forbidden
        statusResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SecurityScenario4_AnonymousUser_AttemptingRestaurantManagement_ShouldReturn401Unauthorized()
    {
        // Arrange
        var anonClient = _factory.CreateClient();

        // Act
        var response = await anonClient.PostAsJsonAsync("/api/v1/restaurants", new CreateRestaurantRequest("Anon Rest", "Desc", "+1234567890", "anon@rest.com"));

        // Assert: 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
