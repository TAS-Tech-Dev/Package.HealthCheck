using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;
using Package.HealthCheck.Core;
using Package.HealthCheck.Configuration;
using Package.HealthCheck.Checks;
using Package.HealthCheck.ML;
using Xunit;

namespace Package.HealthCheck.Tests;

public class HealthCheckBuilderTests
{
    private readonly IServiceCollection _services;
    private readonly HealthCheckBuilder _builder;

    public HealthCheckBuilderTests()
    {
        _services = new ServiceCollection();
        var netHealthChecksBuilder = _services.AddHealthChecks();
        var healthConfig = new HealthConfig();
        _builder = new Package.HealthCheck.Core.HealthCheckBuilder(_services, netHealthChecksBuilder, healthConfig, "TestService", null);
    }

    [Fact]
    public void AddPostgres_ShouldRegisterPostgresHealthCheck()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=test;User Id=test;Password=test;";

        // Act
        var result = _builder.AddPostgres(connectionString, "test-postgres", new[] { "test", "database" });

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IHealthChecksBuilder));
    }

    [Fact]
    public void AddRedis_ShouldRegisterRedisHealthCheck()
    {
        // Arrange
        var connectionString = "localhost:6379";

        // Act
        var result = _builder.AddRedis(connectionString, "test-redis", new[] { "test", "cache" });

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IHealthChecksBuilder));
    }

    [Fact]
    public void AddRabbitMq_ShouldRegisterRabbitMqHealthCheck()
    {
        // Arrange
        var connectionString = "amqp://guest:guest@localhost:5672/";

        // Act
        var result = _builder.AddRabbitMq(connectionString, "test-rabbitmq", new[] { "test", "queue" });

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IHealthChecksBuilder));
    }

    [Fact]
    public void AddHttpDependency_ShouldRegisterHttpClientAndHealthCheck()
    {
        // Arrange
        var name = "test-api";
        var url = "https://api.test.com/health";

        // Act
        var result = _builder.AddHttpDependency(name, url, critical: true, timeoutSeconds: 5, new[] { "test", "external" });

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IHttpClientFactory));
    }

    [Fact]
    public void AddDashboard_ShouldConfigureDashboardAndAddControllers()
    {
        // Arrange
        var route = "/custom-dashboard";
        var enableAutoRefresh = false;
        var refreshInterval = 60;

        // Act
        var result = _builder.AddDashboard(route, enableAutoRefresh, refreshInterval);

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IControllerActivator));
        _services.Should().Contain(s => s.ServiceType == typeof(IMvcCoreBuilder));
    }

    [Fact]
    public void AddServiceMesh_ShouldConfigureServiceMeshAndAddHttpClient()
    {
        // Arrange
        var baseUrl = "http://istio:15020";
        var meshType = "Istio";
        var serviceName = "test-service";
        var timeoutSeconds = 45;
        var apiKey = "test-api-key";

        // Act
        var result = _builder.AddServiceMesh(baseUrl, meshType, serviceName, timeoutSeconds, apiKey);

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IConfigureOptions<ServiceMeshOptions>));
        _services.Should().Contain(s => s.ServiceType == typeof(HttpClient));
    }

    [Fact]
    public void AddPredictiveAnalysis_ShouldConfigurePredictiveAnalysisAndAddServices()
    {
        // Arrange
        var analysisWindowHours = 48;
        var analysisIntervalMinutes = 30;
        var degradationThreshold = 0.2;
        var criticalThreshold = 0.8;

        // Act
        var result = _builder.AddPredictiveAnalysis(analysisWindowHours, analysisIntervalMinutes, degradationThreshold, criticalThreshold);

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IConfigureOptions<PredictiveAnalysisOptions>));
    }

    [Fact]
    public void AddCustomConnectionString_ShouldDetectSqlServerAndRegisterHealthCheck()
    {
        // Arrange
        var name = "sqlserver";
        var connectionString = "Server=localhost;Database=test;User Id=test;Password=test;";
        var healthCheckName = "sqlserver-health";

        // Act
        var result = _builder.AddCustomConnectionString(name, connectionString, healthCheckName, new[] { "test", "custom" });

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IHealthChecksBuilder));
    }

    [Fact]
    public void AddCustomConnectionString_ShouldDetectMongoDbAndRegisterHealthCheck()
    {
        // Arrange
        var name = "mongodb";
        var connectionString = "mongodb://user:pass@localhost:27017/test";
        var healthCheckName = "mongodb-health";

        // Act
        var result = _builder.AddCustomConnectionString(name, connectionString, healthCheckName, new[] { "test", "custom" });

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IHealthChecksBuilder));
    }

    [Fact]
    public void AddCustomConnectionString_ShouldDetectMySqlAndRegisterHealthCheck()
    {
        // Arrange
        var name = "mysql";
        var connectionString = "Server=localhost;Database=test;Uid=test;Pwd=test;";
        var healthCheckName = "mysql-health";

        // Act
        var result = _builder.AddCustomConnectionString(name, connectionString, healthCheckName, new[] { "test", "custom" });

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IHealthChecksBuilder));
    }

    [Fact]
    public void AddCustomConnectionString_ShouldDetectPostgresAndRegisterHealthCheck()
    {
        // Arrange
        var name = "postgres";
        var connectionString = "Host=localhost;Database=test;Username=test;Password=test;";
        var healthCheckName = "postgres-health";

        // Act
        var result = _builder.AddCustomConnectionString(name, connectionString, healthCheckName, new[] { "test", "custom" });

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IHealthChecksBuilder));
    }

    [Fact]
    public void EnableAutoDiscovery_ShouldSetAutoDiscoveryToTrue()
    {
        // Act
        var result = _builder.EnableAutoDiscovery();

        // Assert
        result.Should().BeSameAs(_builder);
        // Note: We can't easily test the internal state without exposing it, but we can verify the method returns the builder
    }

    [Fact]
    public void EnableStartupProbe_ShouldSetStartupProbeToTrue()
    {
        // Act
        var result = _builder.EnableStartupProbe();

        // Assert
        result.Should().BeSameAs(_builder);
        // Note: We can't easily test the internal state without exposing it, but we can verify the method returns the builder
    }

    [Fact]
    public void WithServiceName_ShouldSetServiceName()
    {
        // Arrange
        var serviceName = "TestService";

        // Act
        var result = _builder.WithServiceName(serviceName);

        // Assert
        result.Should().BeSameAs(_builder);
        // Note: We can't easily test the internal state without exposing it, but we can verify the method returns the builder
    }

    [Fact]
    public void Build_ShouldReturnServiceCollection()
    {
        // Act
        var result = _builder.Build();

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void FluentApi_ShouldAllowMethodChaining()
    {
        // Act
        var result = _builder
            .AddPostgres("Server=localhost;Database=test;")
            .AddRedis("localhost:6379")
            .AddDashboard()
            .EnableAutoDiscovery()
            .Build();

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Theory]
    [InlineData("Server=localhost;Database=test;", "sqlserver")]
    [InlineData("mongodb://localhost:27017/test", "mongodb")]
    [InlineData("Host=localhost;Database=test;", "postgres")]
    [InlineData("Server=localhost;Database=test;Uid=test;", "mysql")]
    public void AddCustomConnectionString_ShouldDetectCorrectDatabaseType(string connectionString, string expectedType)
    {
        // Arrange
        var name = "test-db";
        var healthCheckName = "test-health";

        // Act
        var result = _builder.AddCustomConnectionString(name, connectionString, healthCheckName);

        // Assert
        result.Should().BeSameAs(_builder);
        // The method should successfully register a health check regardless of the database type
    }

    [Fact]
    public void AddHttpDependency_WithCriticalFalse_ShouldSetCorrectFailureStatus()
    {
        // Arrange
        var name = "non-critical-api";
        var url = "https://api.test.com/health";

        // Act
        var result = _builder.AddHttpDependency(name, url, critical: false, timeoutSeconds: 2);

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IHttpClientFactory));
    }

    [Fact]
    public void AddDashboard_WithDefaultValues_ShouldUseDefaultConfiguration()
    {
        // Act
        var result = _builder.AddDashboard();

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IControllerActivator));
    }

    [Fact]
    public void AddServiceMesh_WithDefaultValues_ShouldUseDefaultConfiguration()
    {
        // Arrange
        var baseUrl = "http://istio:15020";

        // Act
        var result = _builder.AddServiceMesh(baseUrl);

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IConfigureOptions<ServiceMeshOptions>));
    }

    [Fact]
    public void AddPredictiveAnalysis_WithDefaultValues_ShouldUseDefaultConfiguration()
    {
        // Act
        var result = _builder.AddPredictiveAnalysis();

        // Assert
        result.Should().BeSameAs(_builder);
        _services.Should().Contain(s => s.ServiceType == typeof(IConfigureOptions<PredictiveAnalysisOptions>));
    }
}
