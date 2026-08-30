using Microsoft.AspNetCore.Identity;
using NextDrop.Modules.Identity.Application.Abstractions;
using NextDrop.Modules.Identity.Domain.Aggregates.User;

namespace NextDrop.Modules.Identity.Infrastructure.Services;

public class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}
