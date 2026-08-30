using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Identity.Domain.Entities;

public sealed class EmailVerificationToken : Entity<Guid>
{
    public Aggregates.User.UserId UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }
    public DateTimeOffset? InvalidatedAtUtc { get; private set; }

    private EmailVerificationToken() : base() { }

    internal EmailVerificationToken(
        Guid id,
        Aggregates.User.UserId userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public bool IsActive(DateTimeOffset now) => UsedAtUtc == null && InvalidatedAtUtc == null && now < ExpiresAtUtc;

    internal void MarkAsUsed(DateTimeOffset now)
    {
        UsedAtUtc = now;
    }

    internal void Invalidate(DateTimeOffset now)
    {
        InvalidatedAtUtc = now;
    }
}
