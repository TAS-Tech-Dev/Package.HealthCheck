namespace Package.HealthCheck.Core;

public sealed class HealthDetailsAuthOptions
{
    public bool Enabled { get; set; } = false;
    public string? ApiKey { get; set; }
}

public sealed class HealthPublishOptions
{
    public bool Enabled { get; set; } = false;
    public string? Broker { get; set; }
    public string Exchange { get; set; } = "platform.health";
    public string RoutingKey { get; set; } = "service.status";
}

public sealed class HttpDependencyConfig
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Critical { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 2;
}

public sealed class HealthConfig
{
    public bool EnableStartupProbe { get; set; } = true;
    public bool EnableAutoDiscovery { get; set; } = true;
    public HealthDetailsAuthOptions DetailsEndpointAuth { get; set; } = new();
    public HealthPublishOptions PublishToMessageBus { get; set; } = new();
    public DependenciesConfig Dependencies { get; set; } = new();
    public ServiceMeshConfig? ServiceMesh { get; set; }
    public PredictiveAnalysisConfig? PredictiveAnalysis { get; set; }
    public DashboardConfig? Dashboard { get; set; }
}

public sealed class DependenciesConfig
{
    public ConnectionStringConfig Postgres { get; set; } = new();
    public ConnectionStringConfig Redis { get; set; } = new();
    public ConnectionStringConfig RabbitMq { get; set; } = new();
    public List<HttpDependencyConfig> HttpDependencies { get; set; } = new();
}

public sealed class ConnectionStringConfig
{
    public string? ConnectionString { get; set; }
}

public sealed partial class MegaWishHealthOptions
{
    public string ServiceName { get; set; } = "Service";
    public bool EnableStartupProbe { get; set; } = true;
}

public sealed class HealthEndpointOptions
{
    public bool ProtectDetailsWithApiKey { get; set; } = true;
}

/// <summary>
/// Configuração para Service Mesh.
/// </summary>
public sealed class ServiceMeshConfig
{
    public bool Enabled { get; set; } = false;
    public string BaseUrl { get; set; } = "http://localhost:15020";
    public string MeshType { get; set; } = "Istio";
    public string ServiceName { get; set; } = "unknown";
    public int TimeoutSeconds { get; set; } = 30;
    public bool ReportMetrics { get; set; } = true;
}

/// <summary>
/// Configuração para análise preditiva.
/// </summary>
public sealed class PredictiveAnalysisConfig
{
    public bool Enabled { get; set; } = false;
    public int AnalysisWindowHours { get; set; } = 24;
    public int AnalysisIntervalMinutes { get; set; } = 15;
    public int MinimumDataPoints { get; set; } = 10;
    public double DegradationThreshold { get; set; } = 0.3;
    public double CriticalThreshold { get; set; } = 0.7;
}

/// <summary>
/// Configuração para dashboard integrado.
/// </summary>
public sealed class DashboardConfig
{
    public bool Enabled { get; set; } = true;
    public string Route { get; set; } = "/health-dashboard";
    public bool EnableAutoRefresh { get; set; } = true;
    public int RefreshIntervalSeconds { get; set; } = 30;
}