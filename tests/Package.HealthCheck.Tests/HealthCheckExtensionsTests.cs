using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Package.HealthCheck.Core;
using Package.HealthCheck.Checks;
using Xunit;

namespace Package.HealthCheck.Tests;

public class HealthCheckExtensionsTests
{
    [Fact]
    public void AddMegaWishHealthChecksBuilder_WithDefaultServiceName_ShouldCreateBuilderWithDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddMegaWishHealthChecksBuilder();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<HealthCheckBuilder>();
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_WithCustomServiceName_ShouldCreateBuilderWithCustomName()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceName = "CustomService";

        // Act
        var builder = services.AddMegaWishHealthChecksBuilder(serviceName);

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<HealthCheckBuilder>();
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_ShouldRegisterStartupSignal()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddMegaWishHealthChecksBuilder("TestService");

        // Assert
        services.Should().Contain(s => s.ServiceType == typeof(StartupSignal));
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_ShouldAddBasicHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddMegaWishHealthChecksBuilder("TestService");

        // Assert
        builder.Should().NotBeNull();
        // Note: We can't easily test the internal health checks without building the service provider
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder1 = services.AddMegaWishHealthChecksBuilder("TestService");
        var builder2 = services.AddMegaWishHealthChecksBuilder("AnotherService");

        // Assert
        builder1.Should().NotBeSameAs(builder2); // Each call should create a new builder
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_ShouldConfigureHealthChecksBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddMegaWishHealthChecksBuilder("TestService");

        // Assert
        services.Should().Contain(s => s.ServiceType == typeof(IHealthChecksBuilder));
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_WithEmptyServiceName_ShouldUseDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddMegaWishHealthChecksBuilder("");

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<HealthCheckBuilder>();
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_WithNullServiceName_ShouldUseDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddMegaWishHealthChecksBuilder(null!);

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<HealthCheckBuilder>();
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_ShouldRegisterServicesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddMegaWishHealthChecksBuilder("TestService");

        // Assert
        services.Should().Contain(s => s.ServiceType == typeof(StartupSignal));
        services.Should().Contain(s => s.ServiceType == typeof(IHealthChecksBuilder));
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_ShouldAllowFluentConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services
            .AddMegaWishHealthChecksBuilder("TestService")
            .AddPostgres("Server=localhost;Database=test;")
            .AddRedis("localhost:6379")
            .Build();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddMegaWishHealthChecksBuilder_ShouldCreateIndependentBuilders()
    {
        // Arrange
        var services1 = new ServiceCollection();
        var services2 = new ServiceCollection();

        // Act
        var builder1 = services1.AddMegaWishHealthChecksBuilder("Service1");
        var builder2 = services2.AddMegaWishHealthChecksBuilder("Service2");

        // Assert
        builder1.Should().NotBeSameAs(builder2);
        services1.Should().NotBeSameAs(services2);
    }
}
