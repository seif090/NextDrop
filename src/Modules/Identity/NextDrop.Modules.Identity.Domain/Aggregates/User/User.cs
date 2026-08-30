using NextDrop.Modules.Identity.Domain.Entities;
using NextDrop.Modules.Identity.Domain.Events;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Identity.Domain.Aggregates.User;

public sealed class User : AggregateRoot<UserId>
{
    private readonly List<RefreshToken> _refreshTokens = new();
    private readonly List<EmailVerificationToken> _emailVerificationTokens = new();

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTimeOffset? EmailVerifiedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<EmailVerificationToken> EmailVerificationTokens => _emailVerificationTokens.AsReadOnly();

    private User() : base() { }

    private User(
        UserId id,
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber,
        UserRole role,
        DateTimeOffset now)
        : base(id)
    {
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Role = role;
        Status = AccountStatus.Unverified;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static User Register(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber,
        UserRole role,
        DateTimeOffset now)
    {
        var user = new User(UserId.New(), email, passwordHash, firstName, lastName, phoneNumber, role, now);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(Guid.NewGuid(), now, user.Id, user.Email, user.FirstName));
        return user;
    }

    public Result<EmailVerificationToken> CreateEmailVerificationToken(string tokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset now)
    {
        foreach (var token in _emailVerificationTokens.Where(t => t.IsActive(now)))
        {
            token.Invalidate(now);
        }

        var verificationToken = new EmailVerificationToken(Guid.NewGuid(), Id, tokenHash, expiresAtUtc, now);
        _emailVerificationTokens.Add(verificationToken);

        RaiseDomainEvent(new EmailVerificationRequestedDomainEvent(Guid.NewGuid(), now, Id, Email, tokenHash));
        return Result.Success(verificationToken);
    }

    public Result VerifyEmail(string tokenHash, DateTimeOffset now)
    {
        if (Status == AccountStatus.Active && EmailVerifiedAtUtc.HasValue)
        {
            return Result.Success();
        }

        var activeToken = _emailVerificationTokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.IsActive(now));
        if (activeToken is null)
        {
            return Result.Failure(Error.Validation("EmailVerification.InvalidToken", "Verification token is invalid, expired, or already used."));
        }

        activeToken.MarkAsUsed(now);
        Status = AccountStatus.Active;
        EmailVerifiedAtUtc = now;
        UpdatedAtUtc = now;

        RaiseDomainEvent(new EmailVerifiedDomainEvent(Guid.NewGuid(), now, Id, Email));
        return Result.Success();
    }

    public RefreshToken CreateRefreshToken(string tokenHash, Guid tokenFamilyId, DateTimeOffset expiresAtUtc, DateTimeOffset now)
    {
        var refreshToken = new RefreshToken(Guid.NewGuid(), Id, tokenFamilyId, tokenHash, expiresAtUtc, now);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public Result<RefreshToken> RotateRefreshToken(
        string oldTokenHash,
        string newTokenHash,
        DateTimeOffset newExpiresAtUtc,
        DateTimeOffset now)
    {
        var targetToken = _refreshTokens.FirstOrDefault(t => t.TokenHash == oldTokenHash);
        if (targetToken is null)
        {
            return Result.Failure<RefreshToken>(Error.Unauthorized("RefreshToken.NotFound", "Refresh token was not found."));
        }

        if (targetToken.RevokedAtUtc.HasValue || targetToken.ReplacedByTokenHash != null)
        {
            RevokeTokenFamily(targetToken.TokenFamilyId, now, "Token reuse breach detected");
            return Result.Failure<RefreshToken>(Error.Unauthorized("RefreshToken.ReuseDetected", "Refresh token reuse detected. Access revoked."));
        }

        if (targetToken.IsExpired(now))
        {
            targetToken.Revoke(now);
            return Result.Failure<RefreshToken>(Error.Unauthorized("RefreshToken.Expired", "Refresh token has expired."));
        }

        targetToken.Revoke(now, replacedByTokenHash: newTokenHash);
        var newToken = new RefreshToken(Guid.NewGuid(), Id, targetToken.TokenFamilyId, newTokenHash, newExpiresAtUtc, now);
        _refreshTokens.Add(newToken);

        RaiseDomainEvent(new RefreshTokenRotatedDomainEvent(Guid.NewGuid(), now, Id, targetToken.TokenFamilyId));
        return Result.Success(newToken);
    }

    public Result RevokeRefreshToken(string tokenHash, DateTimeOffset now)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (token is null || !token.IsActive(now))
        {
            return Result.Failure(Error.NotFound("RefreshToken.NotFound", "Active refresh token was not found."));
        }

        token.Revoke(now);
        return Result.Success();
    }

    public void RevokeTokenFamily(Guid tokenFamilyId, DateTimeOffset now, string reason = "Security Revocation")
    {
        var familyTokens = _refreshTokens.Where(t => t.TokenFamilyId == tokenFamilyId && t.RevokedAtUtc == null).ToList();
        foreach (var token in familyTokens)
        {
            token.Revoke(now, isReused: true);
        }

        RaiseDomainEvent(new TokenFamilyRevokedDomainEvent(Guid.NewGuid(), now, Id, tokenFamilyId, reason));
    }

    public void RecordLogin(DateTimeOffset now)
    {
        UpdatedAtUtc = now;
        RaiseDomainEvent(new UserLoggedInDomainEvent(Guid.NewGuid(), now, Id, Email));
    }

    public void UpdatePassword(string newPasswordHash, DateTimeOffset now)
    {
        PasswordHash = newPasswordHash;
        UpdatedAtUtc = now;
        
        foreach (var token in _refreshTokens.Where(t => t.RevokedAtUtc == null))
        {
            token.Revoke(now, isReused: false);
        }
    }
}
