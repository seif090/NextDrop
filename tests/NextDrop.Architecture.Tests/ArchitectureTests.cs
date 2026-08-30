using FluentAssertions;
using NetArchTest.Rules;
using NextDrop.Modules.Identity.Domain.Aggregates.User;
using NextDrop.SharedKernel.Common;
using Xunit;

namespace NextDrop.Architecture.Tests;

public class ArchitectureTests
{
    private static readonly System.Reflection.Assembly SharedKernelAssembly = typeof(Entity<>).Assembly;
    private static readonly System.Reflection.Assembly IdentityDomainAssembly = typeof(User).Assembly;
    private static readonly System.Reflection.Assembly IdentityApplicationAssembly = typeof(NextDrop.Modules.Identity.Application.Commands.RegisterUser.RegisterUserCommand).Assembly;
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
    public void IdentityApplication_ShouldNotDependOnConcreteInfrastructureImplementations()
    {
        var result = Types.InAssembly(IdentityApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAll(
                "NextDrop.Infrastructure",
                "NextDrop.Modules.Identity.Infrastructure",
                "NextDrop.Api")
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
