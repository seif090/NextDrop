using FluentValidation;
using MediatR;
using NextDrop.Modules.Delivery.Application.Abstractions;
using NextDrop.Modules.Delivery.Application.DTOs;
using NextDrop.Modules.Delivery.Domain.Aggregates;
using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.Modules.Orders.Application.Abstractions;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Delivery.Application.Commands;

// 1. REQUEST DELIVERY
public record RequestDeliveryCommand(
    Guid RequesterUserId,
    Guid OrderId) : IRequest<Result<DeliveryDto>>;

public class RequestDeliveryCommandHandler : IRequestHandler<RequestDeliveryCommand, Result<DeliveryDto>>
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RequestDeliveryCommandHandler(
        IDeliveryRepository deliveryRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _deliveryRepository = deliveryRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<DeliveryDto>> Handle(RequestDeliveryCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken);
        if (order == null)
            return Result.Failure<DeliveryDto>(Error.NotFound("Order.NotFound", "Order not found."));

        var existingDelivery = await _deliveryRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        if (existingDelivery != null)
            return MapToDeliveryDto(existingDelivery);

        var deliveryId = DeliveryId.New();
        var deliveryResult = Domain.Aggregates.Delivery.Create(
            deliveryId,
            order.Id,
            order.RestaurantBranchId,
            order.CustomerId,
            _dateTimeProvider.UtcNow);

        if (deliveryResult.IsFailure)
            return Result.Failure<DeliveryDto>(deliveryResult.Error);

        var delivery = deliveryResult.Value;
        delivery.RequestRiderSearch(_dateTimeProvider.UtcNow);

        await _deliveryRepository.AddAsync(delivery, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDeliveryDto(delivery);
    }

    internal static DeliveryDto MapToDeliveryDto(Domain.Aggregates.Delivery delivery)
    {
        return new DeliveryDto(
            delivery.Id.Value,
            delivery.OrderId.Value,
            delivery.BranchId.Value,
            delivery.CustomerId.Value,
            delivery.RiderId?.Value,
            delivery.Status.ToString(),
            delivery.PickedUpAtUtc,
            delivery.DeliveredAtUtc,
            delivery.FailedAtUtc,
            delivery.FailureReason,
            delivery.CreatedAtUtc);
    }
}

// 2. ACCEPT DELIVERY (RIDER)
public record AcceptDeliveryCommand(
    Guid RequesterUserId,
    Guid DeliveryId) : IRequest<Result<DeliveryDto>>;

public class AcceptDeliveryCommandHandler : IRequestHandler<AcceptDeliveryCommand, Result<DeliveryDto>>
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IRiderRepository _riderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptDeliveryCommandHandler(
        IDeliveryRepository deliveryRepository,
        IRiderRepository riderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _deliveryRepository = deliveryRepository;
        _riderRepository = riderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<DeliveryDto>> Handle(AcceptDeliveryCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure<DeliveryDto>(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        if (rider.Status != RiderStatus.Active)
            return Result.Failure<DeliveryDto>(Error.Conflict("Rider.Inactive", "Rider must be Active to accept deliveries."));

        var activeDelivery = await _deliveryRepository.GetActiveDeliveryByRiderIdAsync(rider.Id, cancellationToken);
        if (activeDelivery != null)
            return Result.Failure<DeliveryDto>(Error.Conflict("Rider.AlreadyBusy", "Rider already has an active delivery assigned."));

        var delivery = await _deliveryRepository.GetByIdAsync(new DeliveryId(request.DeliveryId), cancellationToken);
        if (delivery == null)
            return Result.Failure<DeliveryDto>(Error.NotFound("Delivery.NotFound", "Delivery not found."));

        var assignResult = delivery.AssignRider(rider.Id, _dateTimeProvider.UtcNow);
        if (assignResult.IsFailure)
            return Result.Failure<DeliveryDto>(assignResult.Error);

        rider.SetAvailability(RiderAvailabilityStatus.Busy, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return RequestDeliveryCommandHandler.MapToDeliveryDto(delivery);
    }
}

// 3. REJECT DELIVERY
public record RejectDeliveryCommand(
    Guid RequesterUserId,
    Guid DeliveryId,
    string Reason) : IRequest<Result>;

public class RejectDeliveryCommandHandler : IRequestHandler<RejectDeliveryCommand, Result>
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IRiderRepository _riderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RejectDeliveryCommandHandler(
        IDeliveryRepository deliveryRepository,
        IRiderRepository riderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _deliveryRepository = deliveryRepository;
        _riderRepository = riderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RejectDeliveryCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        var delivery = await _deliveryRepository.GetByIdAsync(new DeliveryId(request.DeliveryId), cancellationToken);
        if (delivery == null)
            return Result.Failure(Error.NotFound("Delivery.NotFound", "Delivery not found."));

        var result = delivery.Reject(rider.Id, request.Reason, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        rider.SetAvailability(RiderAvailabilityStatus.Available, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 4. ARRIVE AT RESTAURANT
public record ArriveAtRestaurantCommand(
    Guid RequesterUserId,
    Guid DeliveryId) : IRequest<Result>;

public class ArriveAtRestaurantCommandHandler : IRequestHandler<ArriveAtRestaurantCommand, Result>
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IRiderRepository _riderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArriveAtRestaurantCommandHandler(
        IDeliveryRepository deliveryRepository,
        IRiderRepository riderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _deliveryRepository = deliveryRepository;
        _riderRepository = riderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArriveAtRestaurantCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        var delivery = await _deliveryRepository.GetByIdAsync(new DeliveryId(request.DeliveryId), cancellationToken);
        if (delivery == null)
            return Result.Failure(Error.NotFound("Delivery.NotFound", "Delivery not found."));

        var result = delivery.ArriveAtRestaurant(rider.Id, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 5. CONFIRM PICKUP
public record ConfirmPickupCommand(
    Guid RequesterUserId,
    Guid DeliveryId) : IRequest<Result>;

public class ConfirmPickupCommandHandler : IRequestHandler<ConfirmPickupCommand, Result>
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IRiderRepository _riderRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfirmPickupCommandHandler(
        IDeliveryRepository deliveryRepository,
        IRiderRepository riderRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _deliveryRepository = deliveryRepository;
        _riderRepository = riderRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ConfirmPickupCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        var delivery = await _deliveryRepository.GetByIdAsync(new DeliveryId(request.DeliveryId), cancellationToken);
        if (delivery == null)
            return Result.Failure(Error.NotFound("Delivery.NotFound", "Delivery not found."));

        var result = delivery.ConfirmPickup(rider.Id, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        // Transition Order state to OutForDelivery via application orchestration
        var order = await _orderRepository.GetByIdAsync(delivery.OrderId, cancellationToken);
        if (order != null)
        {
            order.TransitionTo(Orders.Domain.Enums.OrderStatus.OutForDelivery, _dateTimeProvider.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 6. START DELIVERY
public record StartDeliveryCommand(
    Guid RequesterUserId,
    Guid DeliveryId) : IRequest<Result>;

public class StartDeliveryCommandHandler : IRequestHandler<StartDeliveryCommand, Result>
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IRiderRepository _riderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StartDeliveryCommandHandler(
        IDeliveryRepository deliveryRepository,
        IRiderRepository riderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _deliveryRepository = deliveryRepository;
        _riderRepository = riderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(StartDeliveryCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        var delivery = await _deliveryRepository.GetByIdAsync(new DeliveryId(request.DeliveryId), cancellationToken);
        if (delivery == null)
            return Result.Failure(Error.NotFound("Delivery.NotFound", "Delivery not found."));

        var result = delivery.StartDelivery(rider.Id, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 7. COMPLETE DELIVERY
public record CompleteDeliveryCommand(
    Guid RequesterUserId,
    Guid DeliveryId) : IRequest<Result>;

public class CompleteDeliveryCommandHandler : IRequestHandler<CompleteDeliveryCommand, Result>
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IRiderRepository _riderRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CompleteDeliveryCommandHandler(
        IDeliveryRepository deliveryRepository,
        IRiderRepository riderRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _deliveryRepository = deliveryRepository;
        _riderRepository = riderRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CompleteDeliveryCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        var delivery = await _deliveryRepository.GetByIdAsync(new DeliveryId(request.DeliveryId), cancellationToken);
        if (delivery == null)
            return Result.Failure(Error.NotFound("Delivery.NotFound", "Delivery not found."));

        var result = delivery.Complete(rider.Id, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        rider.SetAvailability(RiderAvailabilityStatus.Available, _dateTimeProvider.UtcNow);

        var order = await _orderRepository.GetByIdAsync(delivery.OrderId, cancellationToken);
        if (order != null)
        {
            order.TransitionTo(Orders.Domain.Enums.OrderStatus.Delivered, _dateTimeProvider.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 8. FAIL DELIVERY
public record FailDeliveryCommand(
    Guid RequesterUserId,
    Guid DeliveryId,
    string Reason) : IRequest<Result>;

public class FailDeliveryCommandHandler : IRequestHandler<FailDeliveryCommand, Result>
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IRiderRepository _riderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FailDeliveryCommandHandler(
        IDeliveryRepository deliveryRepository,
        IRiderRepository riderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _deliveryRepository = deliveryRepository;
        _riderRepository = riderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(FailDeliveryCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        var delivery = await _deliveryRepository.GetByIdAsync(new DeliveryId(request.DeliveryId), cancellationToken);
        if (delivery == null)
            return Result.Failure(Error.NotFound("Delivery.NotFound", "Delivery not found."));

        var result = delivery.Fail(request.Reason, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        rider.SetAvailability(RiderAvailabilityStatus.Available, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
