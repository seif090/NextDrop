using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NextDrop.Modules.Customers.Application.Abstractions;
using NextDrop.Modules.Customers.Application.Commands;
using NextDrop.Modules.Customers.Infrastructure.Persistence.Repositories;

namespace NextDrop.Modules.Customers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomersModule(this IServiceCollection services)
    {
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOrUpdateCustomerProfileCommand).Assembly));
        services.AddValidatorsFromAssembly(typeof(CreateOrUpdateCustomerProfileCommandValidator).Assembly);

        return services;
    }
}
