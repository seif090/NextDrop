using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Catalog.Domain.ValueObjects;

namespace NextDrop.Modules.Catalog.Infrastructure.Persistence.Repositories;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly NextDropDbContext _context;

    public MenuItemRepository(NextDropDbContext context)
    {
        _context = context;
    }

    public async Task<MenuItem?> GetByIdAsync(MenuItemId id, CancellationToken cancellationToken = default)
    {
        return await _context.MenuItems
            .Include(m => m.Variants)
            .Include(m => m.ModifierGroups)
                .ThenInclude(mg => mg.Options)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<List<MenuItem>> GetByCatalogIdAsync(CatalogId catalogId, CancellationToken cancellationToken = default)
    {
        var categoryIds = await _context.Catalogs
            .Where(c => c.Id == catalogId)
            .SelectMany(c => c.Categories.Select(cat => cat.Id))
            .ToListAsync(cancellationToken);

        return await _context.MenuItems
            .Include(m => m.Variants)
            .Include(m => m.ModifierGroups)
                .ThenInclude(mg => mg.Options)
            .Where(m => categoryIds.Contains(m.CategoryId) && m.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByCatalogIdAsync(CatalogId catalogId, CancellationToken cancellationToken = default)
    {
        var categoryIds = await _context.Catalogs
            .Where(c => c.Id == catalogId)
            .SelectMany(c => c.Categories.Select(cat => cat.Id))
            .ToListAsync(cancellationToken);

        return await _context.MenuItems
            .CountAsync(m => categoryIds.Contains(m.CategoryId) && m.IsActive, cancellationToken);
    }

    public async Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken = default)
    {
        await _context.MenuItems.AddAsync(menuItem, cancellationToken);
    }
}
