using FluentAssertions;
using NetArchTest.Rules;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Customers.Domain.Aggregates;
using NextDrop.Modules.Delivery.Domain.Aggregates;
using NextDrop.Modules.Identity.Domain.Aggregates.User;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.SharedKernel.Common;
using Xunit;

namespace NextDrop.Architecture.Tests;

public class ArchitectureTests
{
    private static readonly System.Reflection.Assembly SharedKernelAssembly = typeof(Entity<>).Assembly;
    private static readonly System.Reflection.Assembly IdentityDomainAssembly = typeof(User).Assembly;
    private static readonly System.Reflection.Assembly IdentityApplicationAssembly = typeof(NextDrop.Modules.Identity.Application.Commands.RegisterUser.RegisterUserCommand).Assembly;
    private static readonly System.Reflection.Assembly CustomersDomainAssembly = typeof(Customer).Assembly;
    private static readonly System.Reflection.Assembly RestaurantsDomainAssembly = typeof(Restaurant).Assembly;
    private static readonly System.Reflection.Assembly CatalogDomainAssembly = typeof(Catalog).Assembly;
    private static readonly System.Reflection.Assembly OrdersDomainAssembly = typeof(Order).Assembly;
    private static readonly System.Reflection.Assembly OrdersApplicationAssembly = typeof(NextDrop.Modules.Orders.Application.Commands.CreateCartCommand).Assembly;
    private static readonly System.Reflection.Assembly DeliveryDomainAssembly = typeof(Delivery).Assembly;
    private static readonly System.Reflection.Assembly DeliveryApplicationAssembly = typeof(NextDrop.Modules.Delivery.Application.Commands.CreateRiderCommand).Assembly;
    private static readonly System.Reflection.Assembly PaymentsDomainAssembly = typeof(NextDrop.Modules.Payments.Domain.Aggregates.Payment).Assembly;
    private static readonly System.Reflection.Assembly PaymentsApplicationAssembly = typeof(NextDrop.Modules.Payments.Application.Commands.CheckoutCommand).Assembly;
    private static readonly System.Reflection.Assembly NotificationsDomainAssembly = typeof(NextDrop.Modules.Notifications.Domain.Aggregates.Notification).Assembly;
    private static readonly System.Reflection.Assembly NotificationsApplicationAssembly = typeof(NextDrop.Modules.Notifications.Application.Commands.CreateNotificationCommand).Assembly;
    private static readonly System.Reflection.Assembly DiscoveryDomainAssembly = typeof(NextDrop.Modules.Discovery.Domain.Enums.DiscoverySort).Assembly;
    private static readonly System.Reflection.Assembly DiscoveryApplicationAssembly = typeof(NextDrop.Modules.Discovery.Application.Queries.GetPublicRestaurantsQuery).Assembly;
    private static readonly System.Reflection.Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void SharedKernel_ShouldNotHaveDependencyOnExternalInfrastructureOrFrameworks()
    {
        var result = Types.InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "StackExchange.Redis",
                "RabbitMQ.Client",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IdentityDomain_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(IdentityDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Identity.Infrastructure",
                "NextDrop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CustomersDomain_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(CustomersDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Customers.Infrastructure",
                "NextDrop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void RestaurantsDomain_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(RestaurantsDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Restaurants.Infrastructure",
                "NextDrop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CatalogDomain_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(CatalogDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Catalog.Infrastructure",
                "NextDrop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void OrdersDomain_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(OrdersDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Orders.Infrastructure",
                "NextDrop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void DeliveryDomain_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(DeliveryDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Delivery.Infrastructure",
                "NextDrop.Api",
                "Microsoft.EntityFrameworkCore",
                "StackExchange.Redis")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void DeliveryApplication_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(DeliveryApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Delivery.Infrastructure",
                "NextDrop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void PaymentsDomain_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(PaymentsDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Payments.Infrastructure",
                "NextDrop.Api",
                "Microsoft.EntityFrameworkCore",
                "StackExchange.Redis")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void PaymentsApplication_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(PaymentsApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Payments.Infrastructure",
                "NextDrop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void NotificationsDomain_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(NotificationsDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Notifications.Infrastructure",
                "NextDrop.Api",
                "Microsoft.EntityFrameworkCore",
                "StackExchange.Redis",
                "RabbitMQ.Client")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void NotificationsApplication_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(NotificationsApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Notifications.Infrastructure",
                "NextDrop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void DiscoveryDomain_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(DiscoveryDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Discovery.Infrastructure",
                "NextDrop.Api",
                "Microsoft.EntityFrameworkCore",
                "StackExchange.Redis")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void DiscoveryApplication_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(DiscoveryApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Discovery.Infrastructure",
                "NextDrop.Api",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
