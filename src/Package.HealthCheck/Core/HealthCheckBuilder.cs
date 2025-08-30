using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Package.HealthCheck.Checks;
using Package.HealthCheck.Discovery;
using Package.HealthCheck.ML;

namespace Package.HealthCheck.Core;

/// <summary>
/// Configurações sensíveis que devem ser definidas via código.
/// </summary>
public sealed class SensitiveHealthConfig
{
    public string? PostgresConnectionString { get; set; }
    public string? RedisConnectionString { get; set; }
    public string? RabbitMqConnectionString { get; set; }
    public string? ServiceMeshApiKey { get; set; }
    public string? DashboardApiKey { get; set; }
    public Dictionary<string, string> CustomConnectionStrings { get; set; } = new();
    public List<HttpDependencyConfig> SecureHttpDependencies { get; set; } = new();
}

/// <summary>
/// Builder fluente para configuração de health checks.
/// </summary>
public sealed class HealthCheckBuilder
{
    private readonly IServiceCollection _services;
    private readonly IHealthChecksBuilder _healthChecksBuilder;
    
    /// <summary>
    /// Obtém o IHealthChecksBuilder interno para uso em testes e extensões.
    /// </summary>
    public IHealthChecksBuilder HealthChecksBuilder => _healthChecksBuilder;
    private readonly SensitiveHealthConfig _sensitiveConfig;
    private readonly HealthConfig _healthConfig;
    private readonly ILogger? _logger;
    private string _serviceName;

    public HealthCheckBuilder(IServiceCollection services, IHealthChecksBuilder healthChecksBuilder, HealthConfig healthConfig, string serviceName = "Service", ILogger? logger = null)
    {
        _services = services;
        _healthChecksBuilder = healthChecksBuilder;
        _healthConfig = healthConfig;
        _sensitiveConfig = new SensitiveHealthConfig();
        _logger = logger;
        _serviceName = serviceName;
    }

    /// <summary>
    /// Adiciona configuração de banco de dados PostgreSQL.
    /// </summary>
    public HealthCheckBuilder AddPostgres(string connectionString, string name = "postgres", string[]? tags = null)
    {
        _sensitiveConfig.PostgresConnectionString = connectionString;
        _healthChecksBuilder.AddNpgSql(connectionString, name: name, tags: tags ?? new[] { "infra", "critical", "ready" });
        return this;
    }

    /// <summary>
    /// Adiciona configuração de Redis.
    /// </summary>
    public HealthCheckBuilder AddRedis(string connectionString, string name = "redis", string[]? tags = null)
    {
        _sensitiveConfig.RedisConnectionString = connectionString;
        _healthChecksBuilder.AddRedis(connectionString, name: name, tags: tags ?? new[] { "infra", "critical", "ready" });
        return this;
    }

    /// <summary>
    /// Adiciona configuração de RabbitMQ.
    /// </summary>
    public HealthCheckBuilder AddRabbitMq(string connectionString, string name = "rabbitmq", string[]? tags = null)
    {
        _sensitiveConfig.RabbitMqConnectionString = connectionString;
        _healthChecksBuilder.AddRabbitMQ(connectionString, name: name, tags: tags ?? new[] { "infra", "critical", "ready" });
        return this;
    }

    /// <summary>
    /// Adiciona dependência HTTP segura.
    /// </summary>
    public HealthCheckBuilder AddHttpDependency(string name, string url, bool critical = true, int timeoutSeconds = 2, string[]? tags = null)
    {
        var httpDep = new HttpDependencyConfig
        {
            Name = name,
            Url = url,
            Critical = critical,
            TimeoutSeconds = timeoutSeconds
        };

        _sensitiveConfig.SecureHttpDependencies.Add(httpDep);

        _services.AddHttpClient($"health-http-{name}")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));

        _healthChecksBuilder.Add(new HealthCheckRegistration(
            name: name,
            factory: sp => new HttpDependencyHealthCheck(sp.GetRequiredService<IHttpClientFactory>().CreateClient($"health-http-{name}"), url, timeoutSeconds),
            failureStatus: critical ? HealthStatus.Unhealthy : HealthStatus.Degraded,
            tags: new[] { "external" }.Concat(critical ? new[] { "critical", "ready" } : new[] { "noncritical" }).Concat(tags ?? Array.Empty<string>()).ToArray()));

        return this;
    }

    /// <summary>
    /// Adiciona dashboard integrado.
    /// </summary>
    public HealthCheckBuilder AddDashboard(string route = "/health-dashboard", bool enableAutoRefresh = true, int refreshIntervalSeconds = 30)
    {
        _healthConfig.Dashboard = new DashboardConfig
        {
            Enabled = true,
            Route = route,
            EnableAutoRefresh = enableAutoRefresh,
            RefreshIntervalSeconds = refreshIntervalSeconds
        };

        _services.AddControllers();
        _services.AddMvcCore();
        return this;
    }

    /// <summary>
    /// Adiciona integração com Service Mesh.
    /// </summary>
    public HealthCheckBuilder AddServiceMesh(string baseUrl, string meshType = "Istio", string serviceName = "unknown", int timeoutSeconds = 30, string? apiKey = null)
    {
        _sensitiveConfig.ServiceMeshApiKey = apiKey;
        _healthConfig.ServiceMesh = new ServiceMeshConfig
        {
            Enabled = true,
            BaseUrl = baseUrl,
            MeshType = meshType,
            ServiceName = serviceName,
            TimeoutSeconds = timeoutSeconds,
            ReportMetrics = true
        };

        _healthConfig.ServiceMesh = new ServiceMeshConfig
        {
            Enabled = true,
            BaseUrl = baseUrl,
            MeshType = meshType,
            ServiceName = serviceName,
            TimeoutSeconds = timeoutSeconds
        };

        _services.AddHttpClient<ServiceMeshHealthCheck>();
        _healthChecksBuilder.AddCheck<ServiceMeshHealthCheck>("service-mesh", tags: new[] { "mesh", "infra", "ready" });

        return this;
    }

    /// <summary>
    /// Adiciona análise preditiva com ML.
    /// </summary>
    public HealthCheckBuilder AddPredictiveAnalysis(int analysisWindowHours = 24, int analysisIntervalMinutes = 15, double degradationThreshold = 0.3, double criticalThreshold = 0.7)
    {
        _healthConfig.PredictiveAnalysis = new PredictiveAnalysisConfig
        {
            Enabled = true,
            AnalysisWindowHours = analysisWindowHours,
            AnalysisIntervalMinutes = analysisIntervalMinutes,
            MinimumDataPoints = 10,
            DegradationThreshold = degradationThreshold,
            CriticalThreshold = criticalThreshold
        };

        // Configuração já definida acima no _healthConfig.PredictiveAnalysis

        _services.TryAddSingleton<IHealthHistoryRepository, DefaultHealthHistoryRepository>();
        _services.TryAddSingleton<IAlertService, DefaultAlertService>();

        _healthChecksBuilder.AddCheck<PredictiveHealthAnalysis>("predictive-analysis", tags: new[] { "ml", "predictive", "ready" });

        return this;
    }

    /// <summary>
    /// Adiciona configuração personalizada de connection string.
    /// </summary>
    public HealthCheckBuilder AddCustomConnectionString(string name, string connectionString, string healthCheckName, string[]? tags = null)
    {
        _sensitiveConfig.CustomConnectionStrings[name] = connectionString;
        
        // Detecção automática do tipo de banco baseada na connection string
        if (connectionString.Contains("Server=") || connectionString.Contains("Data Source="))
        {
            // SQL Server ou similar
            _healthChecksBuilder.AddSqlServer(connectionString, name: healthCheckName, tags: tags ?? new[] { "infra", "custom", "ready" });
        }
        else if (connectionString.Contains("mongodb://") || connectionString.Contains("mongodb+srv://"))
        {
            // MongoDB
            _healthChecksBuilder.AddMongoDb(connectionString, name: healthCheckName, tags: tags ?? new[] { "infra", "custom", "ready" });
        }
        else if (connectionString.Contains("Server=") && (connectionString.Contains("Uid=") || connectionString.Contains("User=")))
        {
            // MySQL
            _healthChecksBuilder.AddMySql(connectionString, name: healthCheckName, tags: tags ?? new[] { "infra", "custom", "ready" });
        }
        else if (connectionString.Contains("Host=") && connectionString.Contains("Database="))
        {
            // PostgreSQL
            _healthChecksBuilder.AddNpgSql(connectionString, name: healthCheckName, tags: tags ?? new[] { "infra", "custom", "ready" });
        }
        else
        {
            // Fallback para health check genérico
            _logger?.LogWarning("Tipo de banco não reconhecido para {ConnectionStringName}. Usando health check genérico.", name);
        }

        return this;
    }

    /// <summary>
    /// Habilita auto-discovery de dependências.
    /// </summary>
    public HealthCheckBuilder EnableAutoDiscovery()
    {
        _healthConfig.EnableAutoDiscovery = true;
        return this;
    }

    /// <summary>
    /// Habilita startup probe.
    /// </summary>
    public HealthCheckBuilder EnableStartupProbe()
    {
        _healthConfig.EnableStartupProbe = true;
        return this;
    }

    /// <summary>
    /// Configura nome do serviço.
    /// </summary>
    public HealthCheckBuilder WithServiceName(string serviceName)
    {
        _serviceName = serviceName;
        return this;
    }

    /// <summary>
    /// Finaliza a configuração e retorna o IServiceCollection.
    /// </summary>
    public IServiceCollection Build()
    {
        // Aplicar auto-discovery se habilitado
        if (_healthConfig.EnableAutoDiscovery)
        {
            var discoveryService = new DependencyDiscoveryService(
                _services.BuildServiceProvider().GetRequiredService<ILogger<DependencyDiscoveryService>>(),
                _services,
                _healthChecksBuilder);
            
            discoveryService.DiscoverAndRegisterHealthChecks();
        }

        return _services;
    }
}


