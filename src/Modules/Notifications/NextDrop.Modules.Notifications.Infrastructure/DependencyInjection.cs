using Microsoft.Extensions.DependencyInjection;
using NextDrop.Modules.Notifications.Application.Abstractions;
using NextDrop.Modules.Notifications.Application.Services;
using NextDrop.Modules.Notifications.Infrastructure.Jobs;
using NextDrop.Modules.Notifications.Infrastructure.Persistence.Repositories;
using NextDrop.Modules.Notifications.Infrastructure.Services;

namespace NextDrop.Modules.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<IUserNotificationPreferenceRepository, UserNotificationPreferenceRepository>();
        services.AddScoped<IProcessedIntegrationEventRepository, ProcessedIntegrationEventRepository>();

        services.AddScoped<INotificationChannel, InAppNotificationChannel>();
        services.AddScoped<INotificationChannel, DevEmailNotificationChannel>();
        services.AddSingleton<INotificationTemplateRenderer, SimpleTemplateRenderer>();

        services.AddScoped<NotificationDeliveryProcessorJob>();

        return services;
    }
}
