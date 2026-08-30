using FluentValidation;
using MediatR;
using NextDrop.Modules.Identity.Application.Abstractions;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Identity.Application.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var hashedOldToken = _tokenService.HashToken(request.RefreshToken);

        var user = await _userRepository.GetByRefreshTokenHashAsync(hashedOldToken, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("RefreshToken.Invalid", "Invalid or revoked refresh token."));
        }

        var newRawRefreshToken = _tokenService.GenerateSecureToken();
        var newHashedRefreshToken = _tokenService.HashToken(newRawRefreshToken);

        var rotateResult = user.RotateRefreshToken(hashedOldToken, newHashedRefreshToken, now.AddDays(7), now);
        
        // Save state changes (persisting family revocation if breach occurred)
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (rotateResult.IsFailure)
        {
            return Result.Failure<AuthResponse>(rotateResult.Error);
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var accessTokenExpiresAtUtc = now.AddMinutes(15);

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

        return Result.Success(new AuthResponse(accessToken, newRawRefreshToken, accessTokenExpiresAtUtc, userDto));
    }
}
