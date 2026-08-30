using FluentValidation;
using MediatR;
using NextDrop.Modules.Delivery.Application.Abstractions;
using NextDrop.Modules.Delivery.Application.DTOs;
using NextDrop.Modules.Delivery.Domain.Aggregates;
using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Delivery.Application.Commands;

// 1. CREATE RIDER
public record CreateRiderCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    VehicleDto Vehicle) : IRequest<Result<RiderDto>>;

public class CreateRiderCommandValidator : AbstractValidator<CreateRiderCommand>
{
    public CreateRiderCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Vehicle).NotNull();
    }
}

public class CreateRiderCommandHandler : IRequestHandler<CreateRiderCommand, Result<RiderDto>>
{
    private readonly IRiderRepository _riderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRiderCommandHandler(
        IRiderRepository riderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _riderRepository = riderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RiderDto>> Handle(CreateRiderCommand request, CancellationToken cancellationToken)
    {
        var existing = await _riderRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existing != null)
            return Result.Failure<RiderDto>(Error.Conflict("Rider.Exists", "Rider profile already exists for this user."));

        if (!Enum.TryParse<VehicleType>(request.Vehicle.Type, true, out var vType))
            vType = VehicleType.Other;

        var vehicle = new Vehicle(vType, request.Vehicle.PlateNumber, request.Vehicle.Description);
        var riderId = RiderId.New();

        var riderResult = Rider.Create(
            riderId,
            request.UserId,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            vehicle,
            _dateTimeProvider.UtcNow);

        if (riderResult.IsFailure)
            return Result.Failure<RiderDto>(riderResult.Error);

        await _riderRepository.AddAsync(riderResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToRiderDto(riderResult.Value);
    }

    internal static RiderDto MapToRiderDto(Rider rider)
    {
        LocationDto? locDto = null;
        if (rider.CurrentLocation != null)
        {
            var l = rider.CurrentLocation;
            locDto = new LocationDto(l.Latitude, l.Longitude, l.Accuracy, l.Heading, l.Speed, l.RecordedAtUtc);
        }

        var v = rider.Vehicle;
        var vDto = new VehicleDto(v.Type.ToString(), v.PlateNumber, v.Description);

        return new RiderDto(
            rider.Id.Value,
            rider.UserId,
            rider.FirstName,
            rider.LastName,
            rider.PhoneNumber,
            rider.Status.ToString(),
            rider.AvailabilityStatus.ToString(),
            vDto,
            locDto,
            rider.LastLocationUpdatedAt);
    }
}

// 2. ACTIVATE RIDER
public record ActivateRiderCommand(Guid RiderId) : IRequest<Result>;

public class ActivateRiderCommandHandler : IRequestHandler<ActivateRiderCommand, Result>
{
    private readonly IRiderRepository _riderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ActivateRiderCommandHandler(
        IRiderRepository riderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _riderRepository = riderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ActivateRiderCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByIdAsync(new RiderId(request.RiderId), cancellationToken);
        if (rider == null)
            return Result.Failure(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        var result = rider.Activate(_dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 3. SET AVAILABILITY
public record SetRiderAvailabilityCommand(
    Guid RequesterUserId,
    RiderAvailabilityStatus NewStatus) : IRequest<Result<RiderDto>>;

public class SetRiderAvailabilityCommandHandler : IRequestHandler<SetRiderAvailabilityCommand, Result<RiderDto>>
{
    private readonly IRiderRepository _riderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetRiderAvailabilityCommandHandler(
        IRiderRepository riderRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _riderRepository = riderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RiderDto>> Handle(SetRiderAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure<RiderDto>(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        var result = rider.SetAvailability(request.NewStatus, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return Result.Failure<RiderDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateRiderCommandHandler.MapToRiderDto(rider);
    }
}

// 4. UPDATE RIDER LOCATION
public record UpdateRiderLocationCommand(
    Guid RequesterUserId,
    decimal Latitude,
    decimal Longitude,
    double? Accuracy,
    double? Heading,
    double? Speed) : IRequest<Result>;

public class UpdateRiderLocationCommandHandler : IRequestHandler<UpdateRiderLocationCommand, Result>
{
    private readonly IRiderRepository _riderRepository;
    private readonly IRiderLocationCacheService _locationCacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateRiderLocationCommandHandler(
        IRiderRepository riderRepository,
        IRiderLocationCacheService locationCacheService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _riderRepository = riderRepository;
        _locationCacheService = locationCacheService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateRiderLocationCommand request, CancellationToken cancellationToken)
    {
        var rider = await _riderRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (rider == null)
            return Result.Failure(Error.NotFound("Rider.NotFound", "Rider profile not found."));

        var locResult = Location.Create(request.Latitude, request.Longitude, request.Accuracy, request.Heading, request.Speed, _dateTimeProvider.UtcNow);
        if (locResult.IsFailure)
            return locResult;

        var updateResult = rider.UpdateLocation(locResult.Value, _dateTimeProvider.UtcNow);
        if (updateResult.IsFailure)
            return updateResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update Ephemeral Redis Location Cache
        var locDto = new LocationDto(request.Latitude, request.Longitude, request.Accuracy, request.Heading, request.Speed, _dateTimeProvider.UtcNow);
        await _locationCacheService.SetLocationAsync(rider.UserId, locDto, cancellationToken);

        return Result.Success();
    }
}
