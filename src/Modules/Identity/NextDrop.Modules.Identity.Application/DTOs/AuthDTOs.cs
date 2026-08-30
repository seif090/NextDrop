namespace NextDrop.Modules.Identity.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string Role,
    string Status,
    DateTimeOffset? EmailVerifiedAtUtc,
    DateTimeOffset CreatedAtUtc);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    UserDto User);

public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string Status);
