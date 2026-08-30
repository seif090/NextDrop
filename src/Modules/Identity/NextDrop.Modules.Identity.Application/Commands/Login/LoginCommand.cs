using FluentValidation;
using MediatR;
using NextDrop.Modules.Identity.Application.Abstractions;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.Modules.Identity.Domain.Aggregates.User;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Identity.Application.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(user, user.PasswordHash, request.Password);
        if (!isPasswordValid)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (user.Status == AccountStatus.Unverified)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.EmailUnverified", "Your email address has not been verified. Please verify your email before logging in."));
        }

        if (user.Status == AccountStatus.Suspended)
        {
            return Result.Failure<AuthResponse>(
                Error.Forbidden("Auth.AccountSuspended", "Your account has been suspended. Please contact support."));
        }

        // Generate JWT Access Token
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var accessTokenExpiresAtUtc = now.AddMinutes(15);

        // Generate Refresh Token
        var rawRefreshToken = _tokenService.GenerateSecureToken();
        var hashedRefreshToken = _tokenService.HashToken(rawRefreshToken);
        var tokenFamilyId = Guid.NewGuid();

        user.CreateRefreshToken(hashedRefreshToken, tokenFamilyId, now.AddDays(7), now);
        user.RecordLogin(now);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(
            user.Id.Value,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Role.ToString(),
            user.Status.ToString(),
            user.EmailVerifiedAtUtc,
            user.CreatedAtUtc);

        return Result.Success(new AuthResponse(accessToken, rawRefreshToken, accessTokenExpiresAtUtc, userDto));
    }
}
