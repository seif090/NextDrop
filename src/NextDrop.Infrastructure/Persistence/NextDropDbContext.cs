using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence.Interceptors;
using NextDrop.Infrastructure.Persistence.Outbox;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Catalog.Domain.Entities;
using NextDrop.Modules.Identity.Domain.Aggregates.User;
using NextDrop.Modules.Identity.Domain.Entities;
using NextDrop.SharedKernel.Abstractions;

namespace NextDrop.Infrastructure.Persistence;

public class NextDropDbContext : DbContext, IUnitOfWork
{
    private readonly DomainEventsToOutboxInterceptor? _interceptor;

    public NextDropDbContext(DbContextOptions<NextDropDbContext> options, DomainEventsToOutboxInterceptor? interceptor = null)
        : base(options)
    {
        _interceptor = interceptor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<NextDrop.Modules.Customers.Domain.Aggregates.Customer> Customers => Set<NextDrop.Modules.Customers.Domain.Aggregates.Customer>();
    public DbSet<NextDrop.Modules.Restaurants.Domain.Aggregates.Restaurant> Restaurants => Set<NextDrop.Modules.Restaurants.Domain.Aggregates.Restaurant>();
    public DbSet<Catalog> Catalogs => Set<Catalog>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<BranchMenuItemAvailability> BranchMenuItemAvailabilities => Set<BranchMenuItemAvailability>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_interceptor != null)
        {
            optionsBuilder.AddInterceptors(_interceptor);
        }
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NextDropDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
