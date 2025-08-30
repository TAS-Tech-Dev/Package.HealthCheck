using FluentAssertions;
using Package.HealthCheck.Core;
using Xunit;

namespace Package.HealthCheck.Tests;

public class ConfigurationModelsTests
{
    [Fact]
    public void HealthConfig_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new HealthConfig();

        // Assert
        config.EnableStartupProbe.Should().BeTrue();
        config.EnableAutoDiscovery.Should().BeTrue();
        config.DetailsEndpointAuth.Should().NotBeNull();
        config.PublishToMessageBus.Should().NotBeNull();
        config.Dependencies.Should().NotBeNull();
        config.ServiceMesh.Should().BeNull();
        config.PredictiveAnalysis.Should().BeNull();
        config.Dashboard.Should().BeNull();
    }

    [Fact]
    public void HealthConfig_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new HealthConfig();

        // Act
        config.EnableStartupProbe = false;
        config.EnableAutoDiscovery = false;

        // Assert
        config.EnableStartupProbe.Should().BeFalse();
        config.EnableAutoDiscovery.Should().BeFalse();
    }

    [Fact]
    public void DependenciesConfig_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new DependenciesConfig();

        // Assert
        config.Postgres.Should().NotBeNull();
        config.Redis.Should().NotBeNull();
        config.RabbitMq.Should().NotBeNull();
        config.HttpDependencies.Should().NotBeNull();
        config.HttpDependencies.Should().BeEmpty();
    }

    [Fact]
    public void ConnectionStringConfig_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new ConnectionStringConfig();

        // Assert
        config.ConnectionString.Should().BeNull();
    }

    [Fact]
    public void ConnectionStringConfig_ConnectionString_ShouldBeSettable()
    {
        // Arrange
        var config = new ConnectionStringConfig();
        var connectionString = "Server=localhost;Database=test;";

        // Act
        config.ConnectionString = connectionString;

        // Assert
        config.ConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void HttpDependencyConfig_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new HttpDependencyConfig();

        // Assert
        config.Name.Should().BeEmpty();
        config.Url.Should().BeEmpty();
        config.Critical.Should().BeTrue();
        config.TimeoutSeconds.Should().Be(2);
    }

    [Fact]
    public void HttpDependencyConfig_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new HttpDependencyConfig();

        // Act
        config.Name = "test-api";
        config.Url = "https://api.test.com/health";
        config.Critical = false;
        config.TimeoutSeconds = 10;

        // Assert
        config.Name.Should().Be("test-api");
        config.Url.Should().Be("https://api.test.com/health");
        config.Critical.Should().BeFalse();
        config.TimeoutSeconds.Should().Be(10);
    }

    [Fact]
    public void ServiceMeshConfig_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new ServiceMeshConfig();

        // Assert
        config.Enabled.Should().BeFalse();
        config.BaseUrl.Should().Be("http://localhost:15020");
        config.MeshType.Should().Be("Istio");
        config.ServiceName.Should().Be("unknown");
        config.TimeoutSeconds.Should().Be(30);
        config.ReportMetrics.Should().BeTrue();
    }

    [Fact]
    public void ServiceMeshConfig_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new ServiceMeshConfig();

        // Act
        config.Enabled = true;
        config.BaseUrl = "http://istio:15020";
        config.MeshType = "Linkerd";
        config.ServiceName = "test-service";
        config.TimeoutSeconds = 60;
        config.ReportMetrics = false;

        // Assert
        config.Enabled.Should().BeTrue();
        config.BaseUrl.Should().Be("http://istio:15020");
        config.MeshType.Should().Be("Linkerd");
        config.ServiceName.Should().Be("test-service");
        config.TimeoutSeconds.Should().Be(60);
        config.ReportMetrics.Should().BeFalse();
    }

    [Fact]
    public void PredictiveAnalysisConfig_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new PredictiveAnalysisConfig();

        // Assert
        config.Enabled.Should().BeFalse();
        config.AnalysisWindowHours.Should().Be(24);
        config.AnalysisIntervalMinutes.Should().Be(15);
        config.MinimumDataPoints.Should().Be(10);
        config.DegradationThreshold.Should().Be(0.3);
        config.CriticalThreshold.Should().Be(0.7);
    }

    [Fact]
    public void PredictiveAnalysisConfig_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new PredictiveAnalysisConfig();

        // Act
        config.Enabled = true;
        config.AnalysisWindowHours = 48;
        config.AnalysisIntervalMinutes = 30;
        config.MinimumDataPoints = 20;
        config.DegradationThreshold = 0.25;
        config.CriticalThreshold = 0.75;

        // Assert
        config.Enabled.Should().BeTrue();
        config.AnalysisWindowHours.Should().Be(48);
        config.AnalysisIntervalMinutes.Should().Be(30);
        config.MinimumDataPoints.Should().Be(20);
        config.DegradationThreshold.Should().Be(0.25);
        config.CriticalThreshold.Should().Be(0.75);
    }

    [Fact]
    public void DashboardConfig_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new DashboardConfig();

        // Assert
        config.Enabled.Should().BeTrue();
        config.Route.Should().Be("/health-dashboard");
        config.EnableAutoRefresh.Should().BeTrue();
        config.RefreshIntervalSeconds.Should().Be(30);
    }

    [Fact]
    public void DashboardConfig_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new DashboardConfig();

        // Act
        config.Enabled = false;
        config.Route = "/custom-dashboard";
        config.EnableAutoRefresh = false;
        config.RefreshIntervalSeconds = 60;

        // Assert
        config.Enabled.Should().BeFalse();
        config.Route.Should().Be("/custom-dashboard");
        config.EnableAutoRefresh.Should().BeFalse();
        config.RefreshIntervalSeconds.Should().Be(60);
    }

    [Fact]
    public void HealthDetailsAuthOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new HealthDetailsAuthOptions();

        // Assert
        config.Enabled.Should().BeFalse();
        config.ApiKey.Should().BeNull();
    }

    [Fact]
    public void HealthDetailsAuthOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new HealthDetailsAuthOptions();

        // Act
        config.Enabled = true;
        config.ApiKey = "test-api-key";

        // Assert
        config.Enabled.Should().BeTrue();
        config.ApiKey.Should().Be("test-api-key");
    }

    [Fact]
    public void HealthPublishOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new HealthPublishOptions();

        // Assert
        config.Enabled.Should().BeFalse();
        config.Broker.Should().BeNull();
        config.Exchange.Should().Be("platform.health");
        config.RoutingKey.Should().Be("service.status");
    }

    [Fact]
    public void HealthPublishOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new HealthPublishOptions();

        // Act
        config.Enabled = true;
        config.Broker = "rabbitmq";
        config.Exchange = "custom.exchange";
        config.RoutingKey = "custom.routing";

        // Assert
        config.Enabled.Should().BeTrue();
        config.Broker.Should().Be("rabbitmq");
        config.Exchange.Should().Be("custom.exchange");
        config.RoutingKey.Should().Be("custom.routing");
    }

    [Fact]
    public void MegaWishHealthOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new MegaWishHealthOptions();

        // Assert
        config.ServiceName.Should().Be("Service");
        config.EnableStartupProbe.Should().BeTrue();
    }

    [Fact]
    public void MegaWishHealthOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new MegaWishHealthOptions();

        // Act
        config.ServiceName = "TestService";
        config.EnableStartupProbe = false;

        // Assert
        config.ServiceName.Should().Be("TestService");
        config.EnableStartupProbe.Should().BeFalse();
    }

    [Fact]
    public void HealthEndpointOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new HealthEndpointOptions();

        // Assert
        config.ProtectDetailsWithApiKey.Should().BeTrue();
    }

    [Fact]
    public void HealthEndpointOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new HealthEndpointOptions();

        // Act
        config.ProtectDetailsWithApiKey = false;

        // Assert
        config.ProtectDetailsWithApiKey.Should().BeFalse();
    }

    [Fact]
    public void SensitiveHealthConfig_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new SensitiveHealthConfig();

        // Assert
        config.PostgresConnectionString.Should().BeNull();
        config.RedisConnectionString.Should().BeNull();
        config.RabbitMqConnectionString.Should().BeNull();
        config.ServiceMeshApiKey.Should().BeNull();
        config.DashboardApiKey.Should().BeNull();
        config.CustomConnectionStrings.Should().NotBeNull();
        config.CustomConnectionStrings.Should().BeEmpty();
        config.SecureHttpDependencies.Should().NotBeNull();
        config.SecureHttpDependencies.Should().BeEmpty();
    }

    [Fact]
    public void SensitiveHealthConfig_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new SensitiveHealthConfig();

        // Act
        config.PostgresConnectionString = "Server=localhost;Database=test;";
        config.RedisConnectionString = "localhost:6379";
        config.RabbitMqConnectionString = "amqp://localhost:5672/";
        config.ServiceMeshApiKey = "mesh-api-key";
        config.DashboardApiKey = "dashboard-api-key";

        // Assert
        config.PostgresConnectionString.Should().Be("Server=localhost;Database=test;");
        config.RedisConnectionString.Should().Be("localhost:6379");
        config.RabbitMqConnectionString.Should().Be("amqp://localhost:5672/");
        config.ServiceMeshApiKey.Should().Be("mesh-api-key");
        config.DashboardApiKey.Should().Be("dashboard-api-key");
    }

    [Fact]
    public void SensitiveHealthConfig_CustomConnectionStrings_ShouldBeMutable()
    {
        // Arrange
        var config = new SensitiveHealthConfig();

        // Act
        config.CustomConnectionStrings["sqlserver"] = "Server=localhost;Database=test;";
        config.CustomConnectionStrings["mongodb"] = "mongodb://localhost:27017/test";

        // Assert
        config.CustomConnectionStrings.Should().HaveCount(2);
        config.CustomConnectionStrings["sqlserver"].Should().Be("Server=localhost;Database=test;");
        config.CustomConnectionStrings["mongodb"].Should().Be("mongodb://localhost:27017/test");
    }

    [Fact]
    public void SensitiveHealthConfig_SecureHttpDependencies_ShouldBeMutable()
    {
        // Arrange
        var config = new SensitiveHealthConfig();
        var httpDep = new HttpDependencyConfig
        {
            Name = "test-api",
            Url = "https://api.test.com/health",
            Critical = true,
            TimeoutSeconds = 5
        };

        // Act
        config.SecureHttpDependencies.Add(httpDep);

        // Assert
        config.SecureHttpDependencies.Should().HaveCount(1);
        config.SecureHttpDependencies[0].Name.Should().Be("test-api");
        config.SecureHttpDependencies[0].Url.Should().Be("https://api.test.com/health");
        config.SecureHttpDependencies[0].Critical.Should().BeTrue();
        config.SecureHttpDependencies[0].TimeoutSeconds.Should().Be(5);
    }
}
