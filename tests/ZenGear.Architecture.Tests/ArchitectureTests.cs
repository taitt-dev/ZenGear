using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace ZenGear.Architecture.Tests;

/// <summary>
/// Tests to enforce Clean Architecture rules.
/// Ensures dependencies flow inward (API → Application → Infrastructure → Domain).
/// </summary>
public class ArchitectureTests
{
    private const string DomainNamespace = "ZenGear.Domain";
    private const string ApplicationNamespace = "ZenGear.Application";
    private const string InfrastructureNamespace = "ZenGear.Infrastructure";
    private const string ApiNamespace = "ZenGear.Api";

    [Fact]
    public void Domain_ShouldNotHaveDependencyOnOtherLayers()
    {
        // Arrange
        var assembly = typeof(ZenGear.Domain.Common.BaseEntity).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOn(ApplicationNamespace)
            .And()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .And()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not depend on any other layer");
    }

    [Fact]
    public void Application_ShouldNotHaveDependencyOnInfrastructureOrApi()
    {
        // Arrange
        var assembly = typeof(ZenGear.Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .And()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Application layer should not depend on Infrastructure or API layers");
    }

    [Fact]
    public void Infrastructure_ShouldNotHaveDependencyOnApi()
    {
        // Arrange
        var assembly = typeof(ZenGear.Infrastructure.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Infrastructure layer should not depend on API layer");
    }

    [Fact]
    public void Controllers_ShouldHaveSuffixController()
    {
        // Arrange
        var assembly = Assembly.Load("ZenGear.Api");

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace("ZenGear.Api.Controllers")
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All controllers should have 'Controller' suffix");
    }

    [Fact]
    public void Handlers_ShouldHaveSuffixHandler()
    {
        // Arrange
        var assembly = typeof(ZenGear.Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Or()
            .ImplementInterface(typeof(MediatR.IRequestHandler<>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All MediatR handlers should have 'Handler' suffix");
    }

    [Fact]
    public void Validators_ShouldHaveSuffixValidator()
    {
        // Arrange
        var assembly = typeof(ZenGear.Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All validators should have 'Validator' suffix");
    }

    [Fact]
    public void Repositories_ShouldBeInInfrastructureLayer()
    {
        // Arrange
        var assembly = typeof(ZenGear.Infrastructure.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ImplementInterface(typeof(ZenGear.Domain.Repositories.IRepository<>))
            .Should()
            .ResideInNamespace("ZenGear.Infrastructure.Persistence.Repositories")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All repository implementations should be in Infrastructure.Persistence.Repositories");
    }

    [Fact]
    public void DomainEvents_ShouldBeInDomainLayer()
    {
        // Arrange
        var assembly = typeof(ZenGear.Domain.Common.BaseEntity).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ImplementInterface(typeof(ZenGear.Domain.Common.IDomainEvent))
            .Should()
            .ResideInNamespace("ZenGear.Domain.Events")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All domain events should be in Domain.Events namespace");
    }

    [Fact]
    public void Commands_ShouldBeInApplicationLayer()
    {
        // Arrange
        var assembly = typeof(ZenGear.Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .ResideInNamespace("ZenGear.Application.Features")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All commands should be in Application.Features namespace");
    }

    [Fact]
    public void Queries_ShouldBeInApplicationLayer()
    {
        // Arrange
        var assembly = typeof(ZenGear.Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .HaveNameEndingWith("Query")
            .Should()
            .ResideInNamespace("ZenGear.Application.Features")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All queries should be in Application.Features namespace");
    }

    [Fact]
    public void Entities_ShouldBeSealed_OrAbstract()
    {
        // Arrange
        var assembly = typeof(ZenGear.Domain.Common.BaseEntity).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .Inherit(typeof(ZenGear.Domain.Common.BaseEntity))
            .Or()
            .Inherit(typeof(ZenGear.Domain.Common.BaseAuditableEntity))
            .Or()
            .Inherit(typeof(ZenGear.Domain.Common.BaseInternalEntity))
            .Should()
            .BeSealed()
            .Or()
            .BeAbstract()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain entities should be sealed or abstract to prevent inheritance issues");
    }
}
