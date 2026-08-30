using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NextDrop.Api.Authorization;
using NextDrop.Api.Middleware;
using NextDrop.Infrastructure.Caching;
using NextDrop.Infrastructure.Messaging;
using NextDrop.Infrastructure.Outbox;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Infrastructure.Persistence.Interceptors;
using NextDrop.Infrastructure.Services;
using NextDrop.Modules.Identity.Application.Abstractions;
using NextDrop.Modules.Identity.Infrastructure.Persistence.Repositories;
using NextDrop.Modules.Identity.Infrastructure.Services;
using NextDrop.Modules.Customers.Infrastructure;
using NextDrop.Modules.Restaurants.Infrastructure;
using NextDrop.Modules.Catalog.Infrastructure;
using NextDrop.Modules.Orders.Infrastructure;
using NextDrop.Modules.Delivery.Infrastructure;
using NextDrop.Modules.Payments.Infrastructure;
using NextDrop.Modules.Notifications.Infrastructure;
using NextDrop.Modules.Discovery.Infrastructure;
using NextDrop.Modules.Notifications.Application.Abstractions;
using NextDrop.Api.Services;
using NextDrop.Api.Hubs;
using NextDrop.SharedKernel.Abstractions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .Enrich.FromLogContext());

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));

builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasherService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IEmailService, DevEmailService>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGeneratorService>();
builder.Services.AddSingleton<IIdempotencyService, InMemoryIdempotencyService>();

builder.Services.AddScoped<DomainEventsToOutboxInterceptor>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NextDropDbContext>());
builder.Services.AddScoped<IMessagePublisher, RabbitMQPublisher>();
builder.Services.AddScoped<OutboxProcessorJob>();

var dbConnectionString = builder.Configuration["Database:ConnectionString"]
    ?? "Host=localhost;Port=5432;Database=nextdrop_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<NextDropDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<DomainEventsToOutboxInterceptor>();
    options.UseNpgsql(dbConnectionString).AddInterceptors(interceptor);
});

var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(NextDrop.Modules.Identity.Application.Commands.RegisterUser.RegisterUserCommand).Assembly,
        typeof(NextDrop.Modules.Customers.Application.Commands.CreateOrUpdateCustomerProfileCommand).Assembly,
        typeof(NextDrop.Modules.Restaurants.Application.Commands.CreateRestaurantCommand).Assembly,
        typeof(NextDrop.Modules.Catalog.Application.Commands.CreateCatalogCommand).Assembly,
        typeof(NextDrop.Modules.Orders.Application.Commands.CreateCartCommand).Assembly,
        typeof(NextDrop.Modules.Delivery.Application.Commands.CreateRiderCommand).Assembly,
        typeof(NextDrop.Modules.Payments.Application.Commands.CheckoutCommand).Assembly,
        typeof(NextDrop.Modules.Notifications.Application.Commands.CreateNotificationCommand).Assembly,
        typeof(NextDrop.Modules.Discovery.Application.Queries.GetPublicRestaurantsQuery).Assembly));

builder.Services.AddValidatorsFromAssemblies(new[]
{
    typeof(NextDrop.Modules.Identity.Application.Commands.RegisterUser.RegisterUserCommand).Assembly,
    typeof(NextDrop.Modules.Customers.Application.Commands.CreateOrUpdateCustomerProfileCommand).Assembly,
    typeof(NextDrop.Modules.Restaurants.Application.Commands.CreateRestaurantCommand).Assembly,
    typeof(NextDrop.Modules.Catalog.Application.Commands.CreateCatalogCommand).Assembly,
    typeof(NextDrop.Modules.Orders.Application.Commands.CreateCartCommand).Assembly,
    typeof(NextDrop.Modules.Delivery.Application.Commands.CreateRiderCommand).Assembly,
    typeof(NextDrop.Modules.Payments.Application.Commands.CheckoutCommand).Assembly,
    typeof(NextDrop.Modules.Notifications.Application.Commands.CreateNotificationCommand).Assembly,
    typeof(NextDrop.Modules.Discovery.Application.Queries.GetPublicRestaurantsQuery).Assembly
});

builder.Services.AddCustomersModule();
builder.Services.AddRestaurantsModule();
builder.Services.AddCatalogModule();
builder.Services.AddOrdersModule();
builder.Services.AddDeliveryModule();
builder.Services.AddPaymentsModule();
builder.Services.AddNotificationsModule();
builder.Services.AddDiscoveryModule(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddScoped<IRealTimeNotificationPublisher, SignalRRealTimeNotificationPublisher>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddNextDropAuthorizationPolicies();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth-policy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("general-policy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<NextDropDbContext>("database_ready");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NextDrop API", Version = "v1", Description = "Production-grade On-Demand Delivery Marketplace API" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderTrackingHub>("/hubs/orders");

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database_ready")
});

app.MapHealthChecks("/health");

var applyMigrationsOnStartup = builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
if (applyMigrationsOnStartup)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NextDropDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();

public partial class Program { }
