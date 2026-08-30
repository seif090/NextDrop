using FluentAssertions;
using NetArchTest.Rules;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Customers.Domain.Aggregates;
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
                "NextDrop.Api",
                "Microsoft.EntityFrameworkCore")
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
                "NextDrop.Api",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void OrdersApplication_ShouldNotDependOnModuleInfrastructuresOrApi()
    {
        var result = Types.InAssembly(OrdersApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Modules.Orders.Infrastructure",
                "NextDrop.Modules.Customers.Infrastructure",
                "NextDrop.Modules.Restaurants.Infrastructure",
                "NextDrop.Modules.Catalog.Infrastructure",
                "NextDrop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CustomersDomain_ShouldNotDependOnRestaurantsModule()
    {
        var result = Types.InAssembly(CustomersDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "NextDrop.Modules.Restaurants.Domain",
                "NextDrop.Modules.Restaurants.Application",
                "NextDrop.Modules.Restaurants.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void RestaurantsDomain_ShouldNotDependOnCustomersModule()
    {
        var result = Types.InAssembly(RestaurantsDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "NextDrop.Modules.Customers.Domain",
                "NextDrop.Modules.Customers.Application",
                "NextDrop.Modules.Customers.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void ApiControllers_ShouldHaveNameEndingWithController()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
