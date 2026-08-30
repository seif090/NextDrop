using FluentValidation;
using MediatR;
using NextDrop.Modules.Orders.Application.Abstractions;
using NextDrop.Modules.Payments.Application.Abstractions;
using NextDrop.Modules.Payments.Application.DTOs;
using NextDrop.Modules.Payments.Domain.Aggregates;
using NextDrop.Modules.Payments.Domain.Enums;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Application.Commands;

// 1. CONFIRM / CAPTURE PAYMENT
public record ConfirmPaymentCommand(
    Guid RequesterUserId,
    Guid PaymentId) : IRequest<Result<PaymentDto>>;

public class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, Result<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfirmPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        IPaymentProvider paymentProvider,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _paymentProvider = paymentProvider;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PaymentDto>> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(new PaymentId(request.PaymentId), cancellationToken);
        if (payment == null)
            return Result.Failure<PaymentDto>(Error.NotFound("Payment.NotFound", "Payment not found."));

        if (payment.UserId != request.RequesterUserId)
            return Result.Failure<PaymentDto>(Error.Forbidden("Payment.Forbidden", "Not authorized to access this payment."));

        var providerRes = await _paymentProvider.ConfirmPaymentAsync(payment, cancellationToken);
        if (providerRes.IsFailure)
        {
            payment.Fail(providerRes.Error.Description, _dateTimeProvider.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<PaymentDto>(providerRes.Error);
        }

        var captureRes = payment.Capture(providerRes.Value.ProviderPaymentId, _dateTimeProvider.UtcNow);
        if (captureRes.IsFailure)
            return Result.Failure<PaymentDto>(captureRes.Error);

        var order = await _orderRepository.GetByIdAsync(payment.OrderId, cancellationToken);
        if (order != null)
        {
            order.MarkPaid(_dateTimeProvider.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToPaymentDto(payment);
    }

    internal static PaymentDto MapToPaymentDto(Payment payment)
    {
        return new PaymentDto(
            payment.Id.Value,
            payment.OrderId.Value,
            payment.UserId,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.Provider.ToString(),
            payment.ProviderPaymentId,
            payment.CreatedAtUtc,
            payment.CapturedAtUtc);
    }
}

// 2. CANCEL PAYMENT
public record CancelPaymentCommand(
    Guid RequesterUserId,
    Guid PaymentId) : IRequest<Result>;

public class CancelPaymentCommandHandler : IRequestHandler<CancelPaymentCommand, Result>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(new PaymentId(request.PaymentId), cancellationToken);
        if (payment == null)
            return Result.Failure(Error.NotFound("Payment.NotFound", "Payment not found."));

        if (payment.UserId != request.RequesterUserId)
            return Result.Failure(Error.Forbidden("Payment.Forbidden", "Not authorized to access this payment."));

        var result = payment.Cancel(_dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 3. CREATE REFUND
public record CreateRefundCommand(
    Guid RequesterUserId,
    Guid PaymentId,
    decimal Amount,
    string Reason) : IRequest<Result<RefundDto>>;

public class CreateRefundCommandValidator : AbstractValidator<CreateRefundCommand>
{
    public CreateRefundCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}

public class CreateRefundCommandHandler : IRequestHandler<CreateRefundCommand, Result<RefundDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRefundCommandHandler(
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        IOrderRepository orderRepository,
        IPaymentProvider paymentProvider,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _orderRepository = orderRepository;
        _paymentProvider = paymentProvider;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RefundDto>> Handle(CreateRefundCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(new PaymentId(request.PaymentId), cancellationToken);
        if (payment == null)
            return Result.Failure<RefundDto>(Error.NotFound("Payment.NotFound", "Payment not found."));

        if (payment.UserId != request.RequesterUserId)
            return Result.Failure<RefundDto>(Error.Forbidden("Refund.Forbidden", "Not authorized to request refund for this payment."));

        var existingTotalRefunds = await _refundRepository.GetTotalSuccessfulRefundsForPaymentAsync(payment.Id, cancellationToken);
        var refundId = RefundId.New();

        var refundResult = Refund.Create(
            refundId,
            payment,
            request.Amount,
            existingTotalRefunds,
            request.Reason,
            _dateTimeProvider.UtcNow);

        if (refundResult.IsFailure)
            return Result.Failure<RefundDto>(refundResult.Error);

        var refund = refundResult.Value;

        var providerRes = await _paymentProvider.RefundPaymentAsync(payment, refund, cancellationToken);
        if (providerRes.IsFailure)
        {
            refund.Fail(providerRes.Error.Description, _dateTimeProvider.UtcNow);
            await _refundRepository.AddAsync(refund, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<RefundDto>(providerRes.Error);
        }

        var isFullRefund = (existingTotalRefunds + refund.Amount) >= payment.Amount;
        refund.Complete(providerRes.Value.ProviderTransactionId, isFullRefund, _dateTimeProvider.UtcNow);

        if (isFullRefund)
            payment.MarkRefunded(_dateTimeProvider.UtcNow);
        else
            payment.MarkPartiallyRefunded(_dateTimeProvider.UtcNow);

        var order = await _orderRepository.GetByIdAsync(payment.OrderId, cancellationToken);
        if (order != null)
        {
            order.MarkRefunded(_dateTimeProvider.UtcNow);
        }

        await _refundRepository.AddAsync(refund, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefundDto(
            refund.Id.Value,
            refund.PaymentId.Value,
            refund.OrderId.Value,
            refund.UserId,
            refund.Amount,
            refund.Currency,
            refund.Status.ToString(),
            refund.Reason,
            refund.ProviderRefundId,
            refund.CreatedAtUtc,
            refund.CompletedAtUtc);
    }
}

// 4. PROCESS PAYMENT WEBHOOK
public record ProcessPaymentWebhookCommand(
    string Provider,
    string ProviderEventId,
    string EventType,
    string Signature,
    string Payload) : IRequest<Result>;

public class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, Result>
{
    private readonly IWebhookEventRepository _webhookRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProcessPaymentWebhookCommandHandler(
        IWebhookEventRepository webhookRepository,
        IPaymentProvider paymentProvider,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _webhookRepository = webhookRepository;
        _paymentProvider = paymentProvider;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        var isValidSignature = await _paymentProvider.VerifyWebhookSignatureAsync(request.Provider, request.Signature, request.Payload, cancellationToken);
        if (!isValidSignature)
            return Result.Failure(Error.Forbidden("Webhook.InvalidSignature", "Invalid webhook signature."));

        var existingEvent = await _webhookRepository.GetByProviderEventIdAsync(request.Provider, request.ProviderEventId, cancellationToken);
        if (existingEvent != null)
        {
            // Replayed webhook: Ignore safely without duplicate processing
            return Result.Success();
        }

        var webhookId = WebhookEventId.New();
        var payloadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(request.Payload)));

        var webhookResult = WebhookEvent.Create(
            webhookId,
            request.Provider,
            request.ProviderEventId,
            request.EventType,
            payloadHash,
            _dateTimeProvider.UtcNow);

        if (webhookResult.IsFailure)
            return webhookResult;

        var webhook = webhookResult.Value;
        webhook.MarkProcessed(_dateTimeProvider.UtcNow);

        await _webhookRepository.AddAsync(webhook, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
