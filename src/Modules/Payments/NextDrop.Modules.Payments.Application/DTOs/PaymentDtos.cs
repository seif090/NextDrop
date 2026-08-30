namespace NextDrop.Modules.Payments.Application.DTOs;

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string Status,
    string Provider,
    string? ProviderPaymentId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CapturedAtUtc);

public record RefundDto(
    Guid Id,
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string Status,
    string Reason,
    string? ProviderRefundId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public record TransactionalCheckoutResultDto(
    Guid OrderId,
    string OrderNumber,
    Guid PaymentId,
    decimal TotalAmount,
    string PaymentStatus,
    string OrderStatus);
