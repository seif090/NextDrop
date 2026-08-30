using FluentValidation;
using MediatR;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Customers.Application.Abstractions;
using NextDrop.Modules.Customers.Application.DTOs;
using NextDrop.Modules.Customers.Domain.Aggregates;
using NextDrop.Modules.Customers.Domain.ValueObjects;

namespace NextDrop.Modules.Customers.Application.Commands;

// 1. Create or Update Profile
public record CreateOrUpdateCustomerProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string PhoneNumber) : IRequest<Result<CustomerDto>>;

public class CreateOrUpdateCustomerProfileCommandValidator : AbstractValidator<CreateOrUpdateCustomerProfileCommand>
{
    public CreateOrUpdateCustomerProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
    }
}

public class CreateOrUpdateCustomerProfileCommandHandler : IRequestHandler<CreateOrUpdateCustomerProfileCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateOrUpdateCustomerProfileCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CustomerDto>> Handle(CreateOrUpdateCustomerProfileCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var existingCustomer = await _customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (existingCustomer == null)
        {
            var createResult = Customer.Create(
                CustomerId.New(),
                request.UserId,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                now);

            if (createResult.IsFailure)
                return Result.Failure<CustomerDto>(createResult.Error);

            await _customerRepository.AddAsync(createResult.Value, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(MapToDto(createResult.Value));
        }

        var updateResult = existingCustomer.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber, now);
        if (updateResult.IsFailure)
            return Result.Failure<CustomerDto>(updateResult.Error);

        _customerRepository.Update(existingCustomer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(existingCustomer));
    }

    private static CustomerDto MapToDto(Customer c) =>
        new(
            c.Id.Value,
            c.UserId,
            c.FirstName,
            c.LastName,
            c.PhoneNumber,
            new CustomerPreferencesDto(
                c.Preferences.PreferredLanguage,
                c.Preferences.PreferredCurrency,
                c.Preferences.AllowMarketingNotifications,
                c.Preferences.AllowOrderNotifications),
            c.CreatedAtUtc);
}

// 2. Add Address
public record AddCustomerAddressCommand(
    Guid UserId,
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
    bool MakeDefault) : IRequest<Result<CustomerAddressDto>>;

public class AddCustomerAddressCommandValidator : AbstractValidator<AddCustomerAddressCommand>
{
    public AddCustomerAddressCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.District).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m);
    }
}

public class AddCustomerAddressCommandHandler : IRequestHandler<AddCustomerAddressCommand, Result<CustomerAddressDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddCustomerAddressCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CustomerAddressDto>> Handle(AddCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer == null)
            return Result.Failure<CustomerAddressDto>(Error.NotFound("Customer.NotFound", "Customer profile not found. Create profile first."));

        var addressResult = customer.AddAddress(
            CustomerAddressId.New(),
            request.Label,
            request.RecipientName,
            request.PhoneNumber,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.District,
            request.BuildingNumber,
            request.Floor,
            request.Apartment,
            request.Latitude,
            request.Longitude,
            request.MakeDefault,
            _dateTimeProvider.UtcNow);

        if (addressResult.IsFailure)
            return Result.Failure<CustomerAddressDto>(addressResult.Error);

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var a = addressResult.Value;
        return Result.Success(new CustomerAddressDto(
            a.Id.Value, a.Label, a.RecipientName, a.PhoneNumber, a.AddressLine1, a.AddressLine2,
            a.City, a.District, a.BuildingNumber, a.Floor, a.Apartment, a.Latitude, a.Longitude, a.IsDefault, a.IsActive));
    }
}

// 3. Set Default Address
public record SetDefaultCustomerAddressCommand(Guid UserId, Guid AddressId) : IRequest<Result>;

public class SetDefaultCustomerAddressCommandHandler : IRequestHandler<SetDefaultCustomerAddressCommand, Result>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetDefaultCustomerAddressCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SetDefaultCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer == null)
            return Result.Failure(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        var result = customer.SetDefaultAddress(CustomerAddressId.From(request.AddressId), _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 4. Deactivate Address
public record DeactivateCustomerAddressCommand(Guid UserId, Guid AddressId) : IRequest<Result>;

public class DeactivateCustomerAddressCommandHandler : IRequestHandler<DeactivateCustomerAddressCommand, Result>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeactivateCustomerAddressCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeactivateCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer == null)
            return Result.Failure(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        var result = customer.DeactivateAddress(CustomerAddressId.From(request.AddressId), _dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 5. Update Preferences
public record UpdateCustomerPreferencesCommand(
    Guid UserId,
    string PreferredLanguage,
    string PreferredCurrency,
    bool AllowMarketingNotifications,
    bool AllowOrderNotifications) : IRequest<Result>;

public class UpdateCustomerPreferencesCommandHandler : IRequestHandler<UpdateCustomerPreferencesCommand, Result>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCustomerPreferencesCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateCustomerPreferencesCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer == null)
            return Result.Failure(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        var prefs = new CustomerPreferences(
            request.PreferredLanguage,
            request.PreferredCurrency,
            request.AllowMarketingNotifications,
            request.AllowOrderNotifications);

        customer.UpdatePreferences(prefs, _dateTimeProvider.UtcNow);

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 6. Get Profile & Addresses Queries
public record GetCustomerProfileQuery(Guid UserId) : IRequest<Result<CustomerDto>>;

public class GetCustomerProfileQueryHandler : IRequestHandler<GetCustomerProfileQuery, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerProfileQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<CustomerDto>> Handle(GetCustomerProfileQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer == null)
            return Result.Failure<CustomerDto>(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        return Result.Success(new CustomerDto(
            customer.Id.Value,
            customer.UserId,
            customer.FirstName,
            customer.LastName,
            customer.PhoneNumber,
            new CustomerPreferencesDto(
                customer.Preferences.PreferredLanguage,
                customer.Preferences.PreferredCurrency,
                customer.Preferences.AllowMarketingNotifications,
                customer.Preferences.AllowOrderNotifications),
            customer.CreatedAtUtc));
    }
}

public record GetCustomerAddressesQuery(Guid UserId) : IRequest<Result<IReadOnlyList<CustomerAddressDto>>>;

public class GetCustomerAddressesQueryHandler : IRequestHandler<GetCustomerAddressesQuery, Result<IReadOnlyList<CustomerAddressDto>>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerAddressesQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<IReadOnlyList<CustomerAddressDto>>> Handle(GetCustomerAddressesQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer == null)
            return Result.Failure<IReadOnlyList<CustomerAddressDto>>(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        var dtos = customer.Addresses
            .Select(a => new CustomerAddressDto(
                a.Id.Value, a.Label, a.RecipientName, a.PhoneNumber, a.AddressLine1, a.AddressLine2,
                a.City, a.District, a.BuildingNumber, a.Floor, a.Apartment, a.Latitude, a.Longitude, a.IsDefault, a.IsActive))
            .ToList();

        return Result.Success<IReadOnlyList<CustomerAddressDto>>(dtos);
    }
}
