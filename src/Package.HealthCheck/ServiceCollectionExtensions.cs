using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Package.HealthCheck.Attributes;
using Package.HealthCheck.Checks;
using Package.HealthCheck.Configuration;
using Package.HealthCheck.Core;
using Package.HealthCheck.Dashboard;
using Package.HealthCheck.Discovery;
using Package.HealthCheck.ML;

namespace Package.HealthCheck;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Método legado para configuração de health checks (mantido para compatibilidade).
    /// Para novas implementações, use a API fluente: services.AddHealthChecks("ServiceName")...
    /// </summary>
    [Obsolete("Use the fluent API: services.AddHealthChecks(\"ServiceName\").AddPostgres(...).AddDashboard()...")]
    public static IServiceCollection AddMegaWishHealthChecks(this IServiceCollection services, IConfiguration configuration, Action<MegaWishHealthOptions>? configure = null)
    {
        var healthConfig = new HealthConfig();
        configuration.GetSection("HealthCheck").Bind(healthConfig);

        var options = new MegaWishHealthOptions();
        configure?.Invoke(options);
        if (options.EnableStartupProbe != healthConfig.EnableStartupProbe)
        {
            healthConfig.EnableStartupProbe = options.EnableStartupProbe;
        }

        services.TryAddSingleton<StartupSignal>();

        var hcBuilder = services.AddHealthChecks();

        // Core probes tags
        hcBuilder.AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

        if (healthConfig.EnableStartupProbe)
        {
            hcBuilder.AddCheck<StartupGateHealthCheck>("startup", tags: new[] { "startup" });
        }

        // Apply custom registrations from options
        options.ApplyRegistrations(services, hcBuilder);

        // Dependencies from configuration
        if (!string.IsNullOrWhiteSpace(healthConfig.Dependencies.Postgres.ConnectionString))
        {
            hcBuilder.AddNpgSql(healthConfig.Dependencies.Postgres.ConnectionString!, name: "postgres", tags: new[] { "infra", "critical", "ready" });
        }
        if (!string.IsNullOrWhiteSpace(healthConfig.Dependencies.Redis.ConnectionString))
        {
            hcBuilder.AddRedis(healthConfig.Dependencies.Redis.ConnectionString!, name: "redis", tags: new[] { "infra", "critical", "ready" });
        }
        if (!string.IsNullOrWhiteSpace(healthConfig.Dependencies.RabbitMq.ConnectionString))
        {
            hcBuilder.AddRabbitMQ(healthConfig.Dependencies.RabbitMq.ConnectionString!, name: "rabbitmq", tags: new[] { "infra", "critical", "ready" });
        }

        if (healthConfig.Dependencies.HttpDependencies?.Count > 0)
        {
            foreach (var dep in healthConfig.Dependencies.HttpDependencies)
            {
                services.AddHttpClient($"health-http-{dep.Name}")
                    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(Math.Max(1, dep.TimeoutSeconds)));

                hcBuilder.Add(new HealthCheckRegistration(
                    name: dep.Name,
                    factory: sp => new HttpDependencyHealthCheck(sp.GetRequiredService<IHttpClientFactory>().CreateClient($"health-http-{dep.Name}"), dep.Url, dep.TimeoutSeconds),
                    // Critical entries affect readiness (Unhealthy)
                    failureStatus: dep.Critical ? HealthStatus.Unhealthy : HealthStatus.Degraded,
                    tags: new[] { "external" }.Concat(dep.Critical ? new[] { "critical", "ready" } : new[] { "noncritical" }).ToArray()));
            }
        }

        // Basic OTel wiring for health events (trace on state evaluation)
        services.AddOpenTelemetry()
            .WithTracing(b =>
            {
                b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(options.ServiceName));
                b.AddSource("Package.HealthCheck");
            });

        // Background worker for metrics and optional publishing
        services.AddHostedService<Package.HealthCheck.Integration.HealthBackgroundWorker>();

        // Auto-discovery de dependências
        if (healthConfig.EnableAutoDiscovery)
        {
            var discoveryService = new DependencyDiscoveryService(
                services.BuildServiceProvider().GetRequiredService<ILogger<DependencyDiscoveryService>>(),
                services,
                hcBuilder);
            
            discoveryService.DiscoverAndRegisterHealthChecks();
        }

        // Service Mesh HealthCheck (se configurado)
        if (healthConfig.ServiceMesh?.Enabled == true)
        {
            services.Configure<ServiceMeshOptions>(configuration.GetSection("HealthCheck:ServiceMesh"));
            services.AddHttpClient<ServiceMeshHealthCheck>();
            
            hcBuilder.AddCheck<ServiceMeshHealthCheck>(
                "service-mesh",
                tags: new[] { "mesh", "infra", "ready" });
        }

        // Análise preditiva com ML
        if (healthConfig.PredictiveAnalysis?.Enabled == true)
        {
            services.Configure<PredictiveAnalysisOptions>(configuration.GetSection("HealthCheck:PredictiveAnalysis"));
            
            // Registrar interfaces (implementações serão fornecidas pelo usuário)
            services.TryAddSingleton<IHealthHistoryRepository, DefaultHealthHistoryRepository>();
            services.TryAddSingleton<IAlertService, DefaultAlertService>();
            
            hcBuilder.AddCheck<PredictiveHealthAnalysis>(
                "predictive-analysis",
                tags: new[] { "ml", "predictive", "ready" });
        }

        // Dashboard integrado
        if (healthConfig.Dashboard?.Enabled == true)
        {
            services.AddControllers();
            services.AddMvcCore();
        }

        return services;
    }

    /// <summary>
    /// Inicia a configuração de health checks com uma API fluente.
    /// </summary>
    public static HealthCheckBuilder AddMegaWishHealthChecksBuilder(this IServiceCollection services, string serviceName = "Service")
    {
        var healthConfig = new HealthConfig();
        var hcBuilder = services.AddHealthChecks();
        
        // Adicionar health checks básicos
        hcBuilder.AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
        
        services.TryAddSingleton<StartupSignal>();
        
        return new HealthCheckBuilder(services, hcBuilder, healthConfig, serviceName);
    }
}