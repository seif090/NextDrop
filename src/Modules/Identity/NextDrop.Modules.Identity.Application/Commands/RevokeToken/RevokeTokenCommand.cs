using FluentValidation;
using MediatR;
using NextDrop.Modules.Identity.Application.Abstractions;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Identity.Application.Commands.RevokeToken;

public record RevokeTokenCommand(string RefreshToken) : IRequest<Result>;

public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenCommandHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var hashedToken = _tokenService.HashToken(request.RefreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(hashedToken, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("RefreshToken.NotFound", "Refresh token was not found."));
        }

        var result = user.RevokeRefreshToken(hashedToken, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
