using FluentValidation;
using MediatR;
using NextDrop.Modules.Identity.Application.Abstractions;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.Modules.Identity.Domain.Aggregates.User;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Identity.Application.Commands.RegisterUser;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserRole Role = UserRole.Customer) : IRequest<Result<RegisterUserResponse>>;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");
    }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var isUnique = await _userRepository.IsEmailUniqueAsync(normalizedEmail, cancellationToken);
        if (!isUnique)
        {
            return Result.Failure<RegisterUserResponse>(
                Error.Conflict("User.EmailNotUnique", "The specified email address is already registered."));
        }

        // Create user entity (unverified)
        var dummyUserForHashing = User.Register(normalizedEmail, string.Empty, request.FirstName, request.LastName, request.PhoneNumber, request.Role, now);
        var hashedPassword = _passwordHasher.HashPassword(dummyUserForHashing, request.Password);
        
        var user = User.Register(normalizedEmail, hashedPassword, request.FirstName, request.LastName, request.PhoneNumber, request.Role, now);

        // Generate email verification token
        var rawVerificationToken = _tokenService.GenerateSecureToken();
        var hashedVerificationToken = _tokenService.HashToken(rawVerificationToken);

        user.CreateEmailVerificationToken(hashedVerificationToken, now.AddHours(24), now);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch verification email
        await _emailService.SendVerificationEmailAsync(user.Email, rawVerificationToken, cancellationToken);

        return Result.Success(new RegisterUserResponse(user.Id.Value, user.Email, user.Status.ToString()));
    }
}
