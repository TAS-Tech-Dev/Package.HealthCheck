using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Package.HealthCheck.Attributes;
using Package.HealthCheck.Discovery;
using Xunit;

namespace Package.HealthCheck.Tests;

public class AutoDiscoveryTests
{
    private readonly IServiceCollection _services;
    private readonly IHealthChecksBuilder _healthChecksBuilder;
    private readonly DependencyDiscoveryService _discoveryService;

    public AutoDiscoveryTests()
    {
        _services = new ServiceCollection();
        var builder = _services.AddMegaWishHealthChecksBuilder();
        _healthChecksBuilder = _services.AddHealthChecks();
        
        // Mock logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<DependencyDiscoveryService>();
        
        _discoveryService = new DependencyDiscoveryService(logger, _services, _healthChecksBuilder);
    }

    [Fact]
    public void DiscoverAndRegisterHealthChecks_ShouldNotThrowException()
    {
        // Act & Assert
        var action = () => _discoveryService.DiscoverAndRegisterHealthChecks();
        action.Should().NotThrow();
    }

    [Fact]
    public void DiscoverAndRegisterHealthChecks_ShouldCompleteSuccessfully()
    {
        // Act
        _discoveryService.DiscoverAndRegisterHealthChecks();

        // Assert
        // The method should complete without throwing exceptions
        // Note: In a real scenario, this would discover actual services
    }

    [Fact]
    public void DependencyDiscoveryService_Constructor_ShouldAcceptValidParameters()
    {
        // Arrange
        var services = new ServiceCollection();
        var hcBuilder = services.AddMegaWishHealthChecksBuilder();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<DependencyDiscoveryService>();

        // Act & Assert
        var action = () => new DependencyDiscoveryService(logger, services, hcBuilder.HealthChecksBuilder);
        action.Should().NotThrow();
    }

    [Fact]
    public void DependencyDiscoveryService_WithNullLogger_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var hcBuilder = services.AddMegaWishHealthChecksBuilder();

        // Act & Assert
        var action = () => new DependencyDiscoveryService(null!, services, hcBuilder.HealthChecksBuilder);
        action.Should().NotThrow();
    }

    [Fact]
    public void DependencyDiscoveryService_WithNullServices_ShouldNotThrow()
    {
        // Arrange
        var hcBuilder = new ServiceCollection().AddMegaWishHealthChecksBuilder();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<DependencyDiscoveryService>();

        // Act & Assert
        var action = () => new DependencyDiscoveryService(logger, null!, hcBuilder.HealthChecksBuilder);
        action.Should().NotThrow();
    }

    [Fact]
    public void DependencyDiscoveryService_WithNullHealthChecksBuilder_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<DependencyDiscoveryService>();

        // Act & Assert
        var action = () => new DependencyDiscoveryService(logger, services, null!);
        action.Should().NotThrow();
    }
}

// Test classes for auto-discovery
[HealthCheck("test-db", HealthCheckType.Database, tags: new[] { "test", "database" })]
public class TestDbContext
{
    // Mock DbContext for testing
}

[HealthCheck("test-api", HealthCheckType.Http, timeoutSeconds: 5, isCritical: true)]
public class TestApiService
{
    // Mock API service for testing
}

[HealthCheck("test-queue", HealthCheckType.MessageQueue, tags: new[] { "test", "queue" })]
public class TestQueueService
{
    // Mock queue service for testing
}

public class TestServiceWithoutAttribute
{
    // Service without HealthCheck attribute
}

[HealthCheck("test-cache", HealthCheckType.Cache, tags: new[] { "test", "cache" })]
public class TestCacheService
{
    // Mock cache service for testing
}

[HealthCheck("test-filesystem", HealthCheckType.FileSystem, tags: new[] { "test", "filesystem" })]
public class TestFileSystemService
{
    // Mock file system service for testing
}

[HealthCheck("test-memory", HealthCheckType.Memory, tags: new[] { "test", "memory" })]
public class TestMemoryService
{
    // Mock memory service for testing
}

[HealthCheck("test-cpu", HealthCheckType.Cpu, tags: new[] { "test", "cpu" })]
public class TestCpuService
{
    // Mock CPU service for testing
}

[HealthCheck("test-network", HealthCheckType.Network, tags: new[] { "test", "network" })]
public class TestNetworkService
{
    // Mock network service for testing
}

[HealthCheck("test-custom", HealthCheckType.Custom, tags: new[] { "test", "custom" })]
public class TestCustomService
{
    // Mock custom service for testing
}
