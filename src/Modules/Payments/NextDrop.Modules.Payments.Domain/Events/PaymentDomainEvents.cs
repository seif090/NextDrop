using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Domain.Events;

public record PaymentCreatedDomainEvent(
    PaymentId PaymentId,
    OrderId OrderId,
    decimal Amount) : IDomainEvent;

public record PaymentAuthorizedDomainEvent(
    PaymentId PaymentId,
    OrderId OrderId,
    string ProviderPaymentId) : IDomainEvent;

public record PaymentCapturedDomainEvent(
    PaymentId PaymentId,
    OrderId OrderId,
    decimal Amount) : IDomainEvent;

public record PaymentFailedDomainEvent(
    PaymentId PaymentId,
    OrderId OrderId,
    string Reason) : IDomainEvent;

public record PaymentCancelledDomainEvent(
    PaymentId PaymentId,
    OrderId OrderId) : IDomainEvent;

public record PaymentRefundedDomainEvent(
    PaymentId PaymentId,
    RefundId RefundId,
    decimal RefundAmount,
    bool IsFullRefund) : IDomainEvent;
