using NextDrop.Modules.Identity.Domain.Aggregates.User;

namespace NextDrop.Modules.Identity.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
}
