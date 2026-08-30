using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextDrop.Modules.Discovery.Application.Abstractions;
using NextDrop.Modules.Discovery.Application.Validators;
using NextDrop.Modules.Discovery.Infrastructure.Services;

namespace NextDrop.Modules.Discovery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDiscoveryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDiscoveryReadService, DiscoveryReadService>();
        services.AddScoped<IDiscoveryCacheService, DiscoveryCacheService>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Application.Queries.GetPublicRestaurantsQuery).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(GetPublicRestaurantsQueryValidator).Assembly);

        return services;
    }
}
