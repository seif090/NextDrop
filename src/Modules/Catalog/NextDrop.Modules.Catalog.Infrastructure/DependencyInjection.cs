using Microsoft.Extensions.DependencyInjection;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Catalog.Infrastructure.Persistence.Repositories;
using NextDrop.Modules.Catalog.Infrastructure.Services;

namespace NextDrop.Modules.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IBranchMenuItemAvailabilityRepository, BranchMenuItemAvailabilityRepository>();
        services.AddScoped<ICatalogCacheService, CatalogCacheService>();

        return services;
    }
}
