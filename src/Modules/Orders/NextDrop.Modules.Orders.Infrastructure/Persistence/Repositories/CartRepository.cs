using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Orders.Application.Abstractions;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Orders.Domain.ValueObjects;

namespace NextDrop.Modules.Orders.Infrastructure.Persistence.Repositories;

public class CartRepository : ICartRepository
{
    private readonly NextDropDbContext _context;

    public CartRepository(NextDropDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByIdAsync(CartId id, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Cart?> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
    }

    public void Delete(Cart cart)
    {
        _context.Carts.Remove(cart);
    }
}
