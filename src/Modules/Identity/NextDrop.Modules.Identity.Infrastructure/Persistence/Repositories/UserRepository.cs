using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Identity.Application.Abstractions;
using NextDrop.Modules.Identity.Domain.Aggregates.User;

namespace NextDrop.Modules.Identity.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly NextDropDbContext _context;

    public UserRepository(NextDropDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.EmailVerificationTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.EmailVerificationTokens)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.EmailVerificationTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.TokenHash == tokenHash), cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return !await _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        foreach (var rt in user.RefreshTokens)
        {
            var entry = _context.Entry(rt);
            if (entry.State == EntityState.Detached || (entry.State == EntityState.Modified && !_context.RefreshTokens.Any(x => x.Id == rt.Id)))
            {
                entry.State = EntityState.Added;
            }
        }

        foreach (var evt in user.EmailVerificationTokens)
        {
            var entry = _context.Entry(evt);
            if (entry.State == EntityState.Detached || (entry.State == EntityState.Modified && !_context.EmailVerificationTokens.Any(x => x.Id == evt.Id)))
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
