using Microsoft.Extensions.DependencyInjection;
using NextDrop.Modules.Delivery.Application.Abstractions;
using NextDrop.Modules.Delivery.Application.Services;
using NextDrop.Modules.Delivery.Infrastructure.Persistence.Repositories;
using NextDrop.Modules.Delivery.Infrastructure.Services;

namespace NextDrop.Modules.Delivery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDeliveryModule(this IServiceCollection services)
    {
        services.AddScoped<IRiderRepository, RiderRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddSingleton<IDistanceCalculator, HaversineDistanceCalculator>();
        services.AddScoped<IRiderLocationCacheService, RiderLocationCacheService>();

        return services;
    }
}
