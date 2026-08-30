using MediatR;
using NextDrop.Modules.Identity.Application.Abstractions;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.Modules.Identity.Domain.Aggregates.User;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Identity.Application.Queries.GetCurrentUser;

public record GetCurrentUserQuery(UserId UserId) : IRequest<Result<UserDto>>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserDto>(Error.NotFound("User.NotFound", "User record was not found."));
        }

        var dto = new UserDto(
            user.Id.Value,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Role.ToString(),
            user.Status.ToString(),
            user.EmailVerifiedAtUtc,
            user.CreatedAtUtc);

        return Result.Success(dto);
    }
}
