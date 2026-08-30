using FluentAssertions;
using Microsoft.Extensions.Options;
using NextDrop.Modules.Identity.Domain.Aggregates.User;
using NextDrop.Modules.Identity.Infrastructure.Services;
using NextDrop.SharedKernel.Abstractions;
using Xunit;

namespace NextDrop.Infrastructure.Tests;

public class InfrastructureTests
{
    private class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void PasswordHasher_ShouldHashAndVerifyPasswordSuccessfully()
    {
        // Arrange
        var hasher = new PasswordHasherService();
        var user = User.Register("test@nextdrop.com", "dummy", "John", "Doe", null, UserRole.Customer, DateTimeOffset.UtcNow);

        // Act
        var hash = hasher.HashPassword(user, "Password123!");
        var isValid = hasher.VerifyPassword(user, hash, "Password123!");
        var isInvalid = hasher.VerifyPassword(user, hash, "WrongPassword!");

        // Assert
        isValid.Should().BeTrue();
        isInvalid.Should().BeFalse();
    }

    [Fact]
    public void TokenService_ShouldGenerateAndHashTokenDeterministically()
    {
        // Arrange
        var service = new TokenService();

        // Act
        var token = service.GenerateSecureToken();
        var hash1 = service.HashToken(token);
        var hash2 = service.HashToken(token);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Length.Should().Be(64); // 32 bytes hex
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void JwtTokenGenerator_ShouldGenerateValidJwtWithLeanClaims()
    {
        // Arrange
        var options = Options.Create(new JwtOptions
        {
            SecretKey = "SuperSecretKeyForNextDropSprint1TestingOnlyMustBeLongEnough!",
            Issuer = "NextDrop",
            Audience = "NextDrop.Clients",
            AccessTokenExpirationMinutes = 15
        });

        var generator = new JwtTokenGeneratorService(options, new FixedDateTimeProvider());
        var user = User.Register("john@nextdrop.com", "hash", "John", "Doe", null, UserRole.Customer, DateTimeOffset.UtcNow);

        // Act
        var jwt = generator.GenerateAccessToken(user);

        // Assert
        jwt.Should().NotBeNullOrEmpty();
        jwt.Split('.').Length.Should().Be(3); // JWT format: header.payload.signature
    }
}
