using NextDrop.Modules.Identity.Domain.Aggregates.User;

namespace NextDrop.Modules.Identity.Application.Abstractions;

public interface IPasswordHasher
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string hashedPassword, string providedPassword);
}
