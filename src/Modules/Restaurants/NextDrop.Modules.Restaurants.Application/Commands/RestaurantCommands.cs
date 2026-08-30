using FluentValidation;
using MediatR;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Application.DTOs;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Restaurants.Application.Commands;

// 1. Create Restaurant
public record CreateRestaurantCommand(
    Guid OwnerUserId,
    string Name,
    string Description,
    string PhoneNumber,
    string Email) : IRequest<Result<RestaurantDto>>;

public class CreateRestaurantCommandValidator : AbstractValidator<CreateRestaurantCommand>
{
    public CreateRestaurantCommandValidator()
    {
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class CreateRestaurantCommandHandler : IRequestHandler<CreateRestaurantCommand, Result<RestaurantDto>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRestaurantCommandHandler(
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RestaurantDto>> Handle(CreateRestaurantCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var restaurantResult = Restaurant.Create(
            RestaurantId.New(),
            request.OwnerUserId,
            request.Name,
            request.Description,
            request.PhoneNumber,
            request.Email,
            now);

        if (restaurantResult.IsFailure)
            return Result.Failure<RestaurantDto>(restaurantResult.Error);

        await _restaurantRepository.AddAsync(restaurantResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(restaurantResult.Value));
    }

    public static RestaurantDto MapToDto(Restaurant r)
    {
        var branches = r.Branches.Select(b => new RestaurantBranchDto(
            b.Id.Value,
            b.RestaurantId.Value,
            b.Name,
            b.PhoneNumber,
            b.AddressLine1,
            b.AddressLine2,
            b.City,
            b.District,
            b.Latitude,
            b.Longitude,
            b.Timezone,
            b.Status.ToString(),
            b.OperatingHours.Select(h => new RestaurantOperatingHoursDto(h.DayOfWeek.ToString(), h.OpenTime.ToString("HH:mm"), h.CloseTime.ToString("HH:mm"), h.IsClosed)).ToList(),
            b.DeliveryZones.Select(z => new RestaurantDeliveryZoneDto(z.Id.Value, z.BranchId.Value, z.Name, z.DeliveryFee, z.MinimumOrderAmount, z.EstimatedDeliveryMinutes, z.IsActive)).ToList()
        )).ToList();

        return new RestaurantDto(
            r.Id.Value,
            r.OwnerUserId,
            r.Name,
            r.Description,
            r.PhoneNumber,
            r.Email,
            r.Status.ToString(),
            branches,
            r.CreatedAtUtc);
    }
}

// 2. Update Restaurant
public record UpdateRestaurantCommand(
    Guid RestaurantId,
    Guid RequesterUserId,
    string Name,
    string Description,
    string PhoneNumber,
    string Email) : IRequest<Result<RestaurantDto>>;

public class UpdateRestaurantCommandHandler : IRequestHandler<UpdateRestaurantCommand, Result<RestaurantDto>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateRestaurantCommandHandler(
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RestaurantDto>> Handle(UpdateRestaurantCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken);
        if (restaurant == null)
            return Result.Failure<RestaurantDto>(Error.NotFound("Restaurant.NotFound", "Restaurant not found."));

        if (!restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager))
            return Result.Failure<RestaurantDto>(Error.Forbidden("Restaurant.Unauthorized", "User is not authorized to manage this restaurant."));

        var result = restaurant.UpdateDetails(request.Name, request.Description, request.PhoneNumber, request.Email, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return Result.Failure<RestaurantDto>(result.Error);

        _restaurantRepository.Update(restaurant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CreateRestaurantCommandHandler.MapToDto(restaurant));
    }
}

// 3. Update Status Command
public record UpdateRestaurantStatusCommand(
    Guid RestaurantId,
    Guid RequesterUserId,
    RestaurantStatus TargetStatus) : IRequest<Result>;

public class UpdateRestaurantStatusCommandHandler : IRequestHandler<UpdateRestaurantStatusCommand, Result>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateRestaurantStatusCommandHandler(
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateRestaurantStatusCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken);
        if (restaurant == null)
            return Result.Failure(Error.NotFound("Restaurant.NotFound", "Restaurant not found."));

        if (!restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner))
            return Result.Failure(Error.Forbidden("Restaurant.Unauthorized", "Only restaurant owner can change status."));

        var now = _dateTimeProvider.UtcNow;
        var transitionResult = request.TargetStatus switch
        {
            RestaurantStatus.Active => restaurant.Activate(now),
            RestaurantStatus.TemporarilyClosed => restaurant.CloseTemporarily(now),
            RestaurantStatus.Suspended => restaurant.Suspend(now),
            RestaurantStatus.Archived => restaurant.Archive(now),
            _ => Result.Failure(Error.Validation("Restaurant.InvalidStatus", "Invalid target status."))
        };

        if (transitionResult.IsFailure)
            return transitionResult;

        _restaurantRepository.Update(restaurant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 4. Create Branch Command
public record CreateBranchCommand(
    Guid RestaurantId,
    Guid RequesterUserId,
    string Name,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string District,
    decimal Latitude,
    decimal Longitude,
    string Timezone) : IRequest<Result<RestaurantBranchDto>>;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, Result<RestaurantBranchDto>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBranchCommandHandler(
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RestaurantBranchDto>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken);
        if (restaurant == null)
            return Result.Failure<RestaurantBranchDto>(Error.NotFound("Restaurant.NotFound", "Restaurant not found."));

        if (!restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager))
            return Result.Failure<RestaurantBranchDto>(Error.Forbidden("Restaurant.Unauthorized", "User is not authorized to create branch for this restaurant."));

        var branchResult = restaurant.AddBranch(
            RestaurantBranchId.New(),
            request.Name,
            request.PhoneNumber,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.District,
            request.Latitude,
            request.Longitude,
            request.Timezone,
            _dateTimeProvider.UtcNow);

        if (branchResult.IsFailure)
            return Result.Failure<RestaurantBranchDto>(branchResult.Error);

        _restaurantRepository.Update(restaurant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var b = branchResult.Value;
        return Result.Success(new RestaurantBranchDto(
            b.Id.Value, b.RestaurantId.Value, b.Name, b.PhoneNumber, b.AddressLine1, b.AddressLine2,
            b.City, b.District, b.Latitude, b.Longitude, b.Timezone, b.Status.ToString(), new List<RestaurantOperatingHoursDto>(), new List<RestaurantDeliveryZoneDto>()));
    }
}

// 5. Set Branch Operating Hours
public record SetBranchOperatingHoursCommand(
    Guid RestaurantId,
    Guid BranchId,
    Guid RequesterUserId,
    List<RestaurantOperatingHoursDto> OperatingHours) : IRequest<Result>;

public class SetBranchOperatingHoursCommandHandler : IRequestHandler<SetBranchOperatingHoursCommand, Result>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetBranchOperatingHoursCommandHandler(
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SetBranchOperatingHoursCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken);
        if (restaurant == null)
            return Result.Failure(Error.NotFound("Restaurant.NotFound", "Restaurant not found."));

        if (!restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager))
            return Result.Failure(Error.Forbidden("Restaurant.Unauthorized", "User is not authorized to manage operating hours."));

        var branch = restaurant.Branches.FirstOrDefault(b => b.Id.Value == request.BranchId);
        if (branch == null)
            return Result.Failure(Error.NotFound("Branch.NotFound", "Branch not found."));

        var domainHours = request.OperatingHours.Select(h =>
        {
            Enum.TryParse<DayOfWeek>(h.DayOfWeek, true, out var day);
            TimeOnly.TryParse(h.OpenTime, out var open);
            TimeOnly.TryParse(h.CloseTime, out var close);
            return h.IsClosed ? RestaurantOperatingHours.Closed(day) : RestaurantOperatingHours.Open(day, open, close);
        }).ToList();

        branch.SetOperatingHours(domainHours, _dateTimeProvider.UtcNow);

        _restaurantRepository.Update(restaurant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 6. Create Delivery Zone
public record CreateDeliveryZoneCommand(
    Guid RestaurantId,
    Guid BranchId,
    Guid RequesterUserId,
    string Name,
    decimal DeliveryFee,
    decimal MinimumOrderAmount,
    int EstimatedDeliveryMinutes) : IRequest<Result<RestaurantDeliveryZoneDto>>;

public class CreateDeliveryZoneCommandHandler : IRequestHandler<CreateDeliveryZoneCommand, Result<RestaurantDeliveryZoneDto>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateDeliveryZoneCommandHandler(
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RestaurantDeliveryZoneDto>> Handle(CreateDeliveryZoneCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken);
        if (restaurant == null)
            return Result.Failure<RestaurantDeliveryZoneDto>(Error.NotFound("Restaurant.NotFound", "Restaurant not found."));

        if (!restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager))
            return Result.Failure<RestaurantDeliveryZoneDto>(Error.Forbidden("Restaurant.Unauthorized", "User is not authorized to manage delivery zones."));

        var branch = restaurant.Branches.FirstOrDefault(b => b.Id.Value == request.BranchId);
        if (branch == null)
            return Result.Failure<RestaurantDeliveryZoneDto>(Error.NotFound("Branch.NotFound", "Branch not found."));

        var zoneResult = branch.AddDeliveryZone(
            RestaurantDeliveryZoneId.New(),
            request.Name,
            request.DeliveryFee,
            request.MinimumOrderAmount,
            request.EstimatedDeliveryMinutes,
            _dateTimeProvider.UtcNow);

        if (zoneResult.IsFailure)
            return Result.Failure<RestaurantDeliveryZoneDto>(zoneResult.Error);

        _restaurantRepository.Update(restaurant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var z = zoneResult.Value;
        return Result.Success(new RestaurantDeliveryZoneDto(z.Id.Value, z.BranchId.Value, z.Name, z.DeliveryFee, z.MinimumOrderAmount, z.EstimatedDeliveryMinutes, z.IsActive));
    }
}

// 7. Add Staff Member
public record AddStaffMemberCommand(
    Guid RestaurantId,
    Guid RequesterUserId,
    Guid TargetUserId,
    RestaurantStaffRole Role) : IRequest<Result<RestaurantStaffMembershipDto>>;

public class AddStaffMemberCommandHandler : IRequestHandler<AddStaffMemberCommand, Result<RestaurantStaffMembershipDto>>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddStaffMemberCommandHandler(
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RestaurantStaffMembershipDto>> Handle(AddStaffMemberCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken);
        if (restaurant == null)
            return Result.Failure<RestaurantStaffMembershipDto>(Error.NotFound("Restaurant.NotFound", "Restaurant not found."));

        if (!restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner))
            return Result.Failure<RestaurantStaffMembershipDto>(Error.Forbidden("Restaurant.Unauthorized", "Only restaurant owner can add staff members."));

        var staffResult = restaurant.AddStaffMember(
            RestaurantStaffMembershipId.New(),
            request.TargetUserId,
            request.Role,
            _dateTimeProvider.UtcNow);

        if (staffResult.IsFailure)
            return Result.Failure<RestaurantStaffMembershipDto>(staffResult.Error);

        _restaurantRepository.Update(restaurant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var s = staffResult.Value;
        return Result.Success(new RestaurantStaffMembershipDto(s.Id.Value, s.RestaurantId.Value, s.UserId, s.Role.ToString(), s.Status.ToString(), s.CreatedAtUtc));
    }
}

// 8. Queries
public record GetRestaurantByIdQuery(Guid RestaurantId) : IRequest<Result<RestaurantDto>>;

public class GetRestaurantByIdQueryHandler : IRequestHandler<GetRestaurantByIdQuery, Result<RestaurantDto>>
{
    private readonly IRestaurantRepository _restaurantRepository;

    public GetRestaurantByIdQueryHandler(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    public async Task<Result<RestaurantDto>> Handle(GetRestaurantByIdQuery request, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken);
        if (restaurant == null)
            return Result.Failure<RestaurantDto>(Error.NotFound("Restaurant.NotFound", "Restaurant not found."));

        return Result.Success(CreateRestaurantCommandHandler.MapToDto(restaurant));
    }
}

public record GetPublicRestaurantsQuery(int Page, int PageSize, string? CityFilter) : IRequest<Result<PagedRestaurantResponse>>;

public class GetPublicRestaurantsQueryHandler : IRequestHandler<GetPublicRestaurantsQuery, Result<PagedRestaurantResponse>>
{
    private readonly IRestaurantRepository _restaurantRepository;

    public GetPublicRestaurantsQueryHandler(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    public async Task<Result<PagedRestaurantResponse>> Handle(GetPublicRestaurantsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > 50 => 50,
            _ => request.PageSize
        };

        var (items, totalCount) = await _restaurantRepository.GetPublicPagedAsync(page, pageSize, request.CityFilter, cancellationToken);

        var summaries = items.Select(r => new RestaurantSummaryDto(
            r.Id.Value,
            r.Name,
            r.Status.ToString(),
            r.Branches.FirstOrDefault(b => b.Status == BranchStatus.Active)?.City,
            r.Branches.Count(b => b.Status == BranchStatus.Active)
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Result.Success(new PagedRestaurantResponse(summaries, page, pageSize, totalCount, totalPages));
    }
}
