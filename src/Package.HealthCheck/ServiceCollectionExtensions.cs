using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Package.HealthCheck.Checks;
using Package.HealthCheck.Core;

namespace Package.HealthCheck;

public static class ServiceCollectionExtensions
{
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

        return services;
    }
}