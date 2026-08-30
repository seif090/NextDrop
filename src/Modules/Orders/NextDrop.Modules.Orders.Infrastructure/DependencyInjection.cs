using Microsoft.Extensions.DependencyInjection;
using NextDrop.Modules.Orders.Application.Abstractions;
using NextDrop.Modules.Orders.Infrastructure.Persistence.Repositories;
using NextDrop.Modules.Orders.Infrastructure.Services;

namespace NextDrop.Modules.Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddScoped<ICartCacheService, CartCacheService>();

        return services;
    }
}
