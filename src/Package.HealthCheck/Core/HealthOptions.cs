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
    public HealthDetailsAuthOptions DetailsEndpointAuth { get; set; } = new();
    public HealthPublishOptions PublishToMessageBus { get; set; } = new();
    public DependenciesConfig Dependencies { get; set; } = new();
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