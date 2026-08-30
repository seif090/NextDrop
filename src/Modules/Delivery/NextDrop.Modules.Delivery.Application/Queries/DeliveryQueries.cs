using MediatR;
using NextDrop.Modules.Delivery.Application.Abstractions;
using NextDrop.Modules.Delivery.Application.Commands;
using NextDrop.Modules.Delivery.Application.DTOs;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Delivery.Application.Queries;

public record GetRiderProfileQuery(Guid RequesterUserId) : IRequest<Result<RiderDto>>;

public class GetRiderProfileQueryHandler : IRequestHandler<GetRiderProfileQuery, Result<RiderDto>>
{
    private readonly IRiderRepository _riderRepository;

    public GetRiderProfileQueryHandler(IRiderRepository riderRepository)
    {
        _riderRepository = riderRepository;
    }

    public async Task<Result<RiderDto>> Handle(GetRiderProfileQuery request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure<RiderDto>(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        return CreateRiderCommandHandler.MapToRiderDto(rider);
    }
}

public record GetDeliveryByIdQuery(Guid RequesterUserId, Guid DeliveryId) : IRequest<Result<DeliveryDto>>;

public class GetDeliveryByIdQueryHandler : IRequestHandler<GetDeliveryByIdQuery, Result<DeliveryDto>>
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IRiderRepository _riderRepository;

    public GetDeliveryByIdQueryHandler(IDeliveryRepository deliveryRepository, IRiderRepository riderRepository)
    {
        _deliveryRepository = deliveryRepository;
        _riderRepository = riderRepository;
    }

    public async Task<Result<DeliveryDto>> Handle(GetDeliveryByIdQuery request, CancellationToken cancellationToken)
    {
        var delivery = await _deliveryRepository.GetByIdAsync(new DeliveryId(request.DeliveryId), cancellationToken);
        if (delivery == null)
            return Result.Failure<DeliveryDto>(Error.NotFound("Delivery.NotFound", "Delivery not found."));

        // Authorization check: Customer owner OR assigned Rider OR Admin
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        bool isAssignedRider = rider != null && delivery.RiderId.HasValue && delivery.RiderId.Value == rider.Id;
        bool isCustomerOwner = delivery.CustomerId.Value == request.RequesterUserId;

        if (!isAssignedRider && !isCustomerOwner)
        {
            // BOLA Protection: Return Forbidden
            return Result.Failure<DeliveryDto>(Error.Forbidden("Delivery.Forbidden", "Not authorized to view this delivery."));
        }

        return RequestDeliveryCommandHandler.MapToDeliveryDto(delivery);
    }
}
