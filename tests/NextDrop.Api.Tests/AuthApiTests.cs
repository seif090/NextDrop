using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Infrastructure.Persistence.Interceptors;
using NextDrop.Modules.Identity.Application.Commands.Login;
using NextDrop.Modules.Identity.Application.Commands.RefreshToken;
using NextDrop.Modules.Identity.Application.Commands.RegisterUser;
using NextDrop.Modules.Identity.Application.Commands.RevokeToken;
using NextDrop.Modules.Identity.Application.Commands.VerifyEmail;
using NextDrop.Modules.Identity.Application.DTOs;
using Xunit;

namespace NextDrop.Api.Tests;

public class AuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    public AuthApiTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task HealthEndpoints_ShouldReturnOkForLiveness()
    {
        var client = _factory.CreateClient();

        var responseLive = await client.GetAsync("/health/live");
        responseLive.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CorrelationId_ShouldBePresentInResponseHeaders()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        var customCorrelationId = Guid.NewGuid().ToString();
        request.Headers.Add("X-Correlation-ID", customCorrelationId);

        var response = await client.SendAsync(request);

        response.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        response.Headers.GetValues("X-Correlation-ID").First().Should().Be(customCorrelationId);
    }

    [Fact]
    public async Task FullAuthFlow_Register_Verify_Login_Refresh_Revoke_ShouldSucceed()
    {
        var client = _factory.CreateClient();

        // 1. Register User
        var registerCommand = new RegisterUserCommand("flowuser@nextdrop.com", "SecurePwd123!", "Flow", "Tester", "+1999888777");
        var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
        {
            Content = JsonContent.Create(registerCommand)
        };
        registerRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var registerResponse = await client.SendAsync(registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registerData = await registerResponse.Content.ReadFromJsonAsync<RegisterUserResponse>();
        registerData.Should().NotBeNull();
        registerData!.Email.Should().Be("flowuser@nextdrop.com");

        // Obtain token from DB directly for verification in test environment
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
            var user = await db.Users.Include(u => u.EmailVerificationTokens).FirstAsync(u => u.Email == "flowuser@nextdrop.com");
            var tokenEntity = user.EmailVerificationTokens.First();

            user.VerifyEmail(tokenEntity.TokenHash, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        // 3. Login
        var loginCommand = new LoginCommand("flowuser@nextdrop.com", "SecurePwd123!");
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var errContent = await loginResponse.Content.ReadAsStringAsync();
            throw new Exception($"Login failed with status {loginResponse.StatusCode}: {errContent}");
        }

        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authData.Should().NotBeNull();
        authData!.AccessToken.Should().NotBeNullOrEmpty();
        authData.RefreshToken.Should().NotBeNullOrEmpty();

        // 4. Refresh Token
        var refreshCommand = new RefreshTokenCommand(authData.RefreshToken);
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshCommand);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var newAuthData = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        newAuthData.Should().NotBeNull();
        newAuthData!.AccessToken.Should().NotBeNullOrEmpty();
        newAuthData.RefreshToken.Should().NotBe(authData.RefreshToken);

        // 5. Revoke Token
        var revokeCommand = new RevokeTokenCommand(newAuthData.RefreshToken);
        var revokeResponse = await client.PostAsJsonAsync("/api/v1/auth/revoke", revokeCommand);
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
