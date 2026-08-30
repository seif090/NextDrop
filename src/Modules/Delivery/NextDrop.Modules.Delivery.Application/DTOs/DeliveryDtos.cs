namespace NextDrop.Modules.Delivery.Application.DTOs;

public record RiderDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Status,
    string AvailabilityStatus,
    VehicleDto Vehicle,
    LocationDto? CurrentLocation,
    DateTimeOffset? LastLocationUpdatedAt);

public record VehicleDto(
    string Type,
    string PlateNumber,
    string? Description);

public record LocationDto(
    decimal Latitude,
    decimal Longitude,
    double? Accuracy,
    double? Heading,
    double? Speed,
    DateTimeOffset RecordedAtUtc);

public record DeliveryDto(
    Guid Id,
    Guid OrderId,
    Guid BranchId,
    Guid CustomerId,
    Guid? RiderId,
    string Status,
    DateTimeOffset? PickedUpAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    DateTimeOffset? FailedAtUtc,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc);
