using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Customers.Application.Abstractions;
using NextDrop.Modules.Customers.Domain.Aggregates;
using NextDrop.Modules.Customers.Domain.ValueObjects;

namespace NextDrop.Modules.Customers.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly NextDropDbContext _context;

    public CustomerRepository(NextDropDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Customer>()
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Customer>()
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Set<Customer>().AddAsync(customer, cancellationToken);
    }

    public void Update(Customer customer)
    {
        foreach (var addr in customer.Addresses)
        {
            var entry = _context.Entry(addr);
            if (entry.State == EntityState.Detached || (entry.State == EntityState.Modified && !_context.Set<Customer>().Any(c => c.Addresses.Any(a => a.Id == addr.Id))))
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
