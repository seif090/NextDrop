using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Application.Commands;
using NextDrop.Modules.Restaurants.Infrastructure.Authorization;
using NextDrop.Modules.Restaurants.Infrastructure.Persistence.Repositories;

namespace NextDrop.Modules.Restaurants.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRestaurantsModule(this IServiceCollection services)
    {
        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IAuthorizationHandler, RestaurantAuthorizationHandler>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateRestaurantCommand).Assembly));
        services.AddValidatorsFromAssembly(typeof(CreateRestaurantCommandValidator).Assembly);

        return services;
    }
}
