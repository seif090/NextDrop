using FluentAssertions;
using NextDrop.Modules.Identity.Domain.Aggregates.User;
using Xunit;

namespace NextDrop.Domain.Tests;

public class UserTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_ShouldCreateUserInUnverifiedState_AndRaiseDomainEvent()
    {
        // Act
        var user = User.Register("test@nextdrop.com", "HashedPwd", "John", "Doe", "+1234567890", UserRole.Customer, FixedNow);

        // Assert
        user.Status.Should().Be(AccountStatus.Unverified);
        user.Email.Should().Be("test@nextdrop.com");
        user.EmailVerifiedAtUtc.Should().BeNull();
        user.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void VerifyEmail_WithValidToken_ShouldActivateUser()
    {
        // Arrange
        var user = User.Register("test@nextdrop.com", "HashedPwd", "John", "Doe", null, UserRole.Customer, FixedNow);
        var tokenResult = user.CreateEmailVerificationToken("token-hash-123", FixedNow.AddHours(24), FixedNow);

        // Act
        var verifyResult = user.VerifyEmail("token-hash-123", FixedNow.AddHours(1));

        // Assert
        verifyResult.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(AccountStatus.Active);
        user.EmailVerifiedAtUtc.Should().Be(FixedNow.AddHours(1));
    }

    [Fact]
    public void RotateRefreshToken_WhenReuseDetected_ShouldRevokeTokenFamily_AndReturnError()
    {
        // Arrange
        var user = User.Register("test@nextdrop.com", "HashedPwd", "John", "Doe", null, UserRole.Customer, FixedNow);
        var familyId = Guid.NewGuid();
        user.CreateRefreshToken("initial-token-hash", familyId, FixedNow.AddDays(7), FixedNow);

        // First Rotation -> Succeds
        var rotate1 = user.RotateRefreshToken("initial-token-hash", "new-token-hash-1", FixedNow.AddDays(7), FixedNow.AddMinutes(5));
        rotate1.IsSuccess.Should().BeTrue();

        // Second Rotation using the ALREADY ROTATED (revoked) initial token -> Token Reuse Attack!
        var rotate2 = user.RotateRefreshToken("initial-token-hash", "attacker-token-hash", FixedNow.AddDays(7), FixedNow.AddMinutes(10));

        // Assert
        rotate2.IsFailure.Should().BeTrue();
        rotate2.Error.Code.Should().Be("RefreshToken.ReuseDetected");

        // Family should be completely revoked
        user.RefreshTokens.Should().AllSatisfy(t => t.RevokedAtUtc.Should().NotBeNull());
    }
}
