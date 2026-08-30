using NextDrop.Modules.Identity.Domain.Aggregates.User;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Identity.Domain.Events;

public sealed record UserRegisteredDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserId UserId,
    string Email,
    string FirstName) : IDomainEvent;

public sealed record EmailVerificationRequestedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserId UserId,
    string Email,
    string VerificationTokenHash) : IDomainEvent;

public sealed record EmailVerifiedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserId UserId,
    string Email) : IDomainEvent;

public sealed record UserLoggedInDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserId UserId,
    string Email) : IDomainEvent;

public sealed record RefreshTokenRotatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserId UserId,
    Guid TokenFamilyId) : IDomainEvent;

public sealed record TokenFamilyRevokedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserId UserId,
    Guid TokenFamilyId,
    string Reason) : IDomainEvent;
