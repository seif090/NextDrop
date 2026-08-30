using MediatR;
using NextDrop.Modules.Payments.Application.Abstractions;
using NextDrop.Modules.Payments.Application.Commands;
using NextDrop.Modules.Payments.Application.DTOs;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Application.Queries;

public record GetPaymentByIdQuery(Guid RequesterUserId, Guid PaymentId) : IRequest<Result<PaymentDto>>;

public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, Result<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PaymentDto>> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(new PaymentId(request.PaymentId), cancellationToken);
        if (payment == null)
            return Result.Failure<PaymentDto>(Error.NotFound("Payment.NotFound", "Payment not found."));

        if (payment.UserId != request.RequesterUserId)
            return Result.Failure<PaymentDto>(Error.Forbidden("Payment.Forbidden", "Not authorized to access this payment."));

        return ConfirmPaymentCommandHandler.MapToPaymentDto(payment);
    }
}
