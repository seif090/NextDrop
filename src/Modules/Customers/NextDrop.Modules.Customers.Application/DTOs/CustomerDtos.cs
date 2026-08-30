namespace NextDrop.Modules.Customers.Application.DTOs;

public record CustomerPreferencesDto(
    string PreferredLanguage,
    string PreferredCurrency,
    bool AllowMarketingNotifications,
    bool AllowOrderNotifications);

public record CustomerAddressDto(
    Guid Id,
    string Label,
    string RecipientName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string District,
    string? BuildingNumber,
    string? Floor,
    string? Apartment,
    decimal Latitude,
    decimal Longitude,
    bool IsDefault,
    bool IsActive);

public record CustomerDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    CustomerPreferencesDto Preferences,
    DateTimeOffset CreatedAtUtc);
