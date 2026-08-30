using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Identity.Domain.Entities;

public sealed class RefreshToken : Entity<Guid>
{
    public Aggregates.User.UserId UserId { get; private set; }
    public Guid TokenFamilyId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public bool IsReused { get; private set; }

    private RefreshToken() : base() { }

    internal RefreshToken(
        Guid id,
        Aggregates.User.UserId userId,
        Guid tokenFamilyId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenFamilyId = tokenFamilyId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public bool IsActive(DateTimeOffset now) => RevokedAtUtc == null && now < ExpiresAtUtc;
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    internal void Revoke(DateTimeOffset revokedAtUtc, string? replacedByTokenHash = null, bool isReused = false)
    {
        RevokedAtUtc = revokedAtUtc;
        ReplacedByTokenHash = replacedByTokenHash;
        if (isReused)
        {
            IsReused = true;
        }
    }
}
