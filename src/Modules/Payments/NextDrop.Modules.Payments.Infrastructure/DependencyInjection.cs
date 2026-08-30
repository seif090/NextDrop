using Microsoft.Extensions.DependencyInjection;
using NextDrop.Modules.Payments.Application.Abstractions;
using NextDrop.Modules.Payments.Infrastructure.Persistence.Repositories;
using NextDrop.Modules.Payments.Infrastructure.Services;

namespace NextDrop.Modules.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
        services.AddScoped<IPaymentProvider, FakePaymentProvider>();

        return services;
    }
}
