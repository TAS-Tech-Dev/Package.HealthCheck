using System.Net.Mime;
using Microsoft.AspNetCore.Routing;
using Package.HealthCheck.Core;

namespace Package.HealthCheck.Endpoints;

public static class HealthEndpointMappings
{
    public static IApplicationBuilder UseMegaWishHealthEndpoints(this IApplicationBuilder app, IConfiguration configuration, Action<HealthEndpointOptions>? configure = null)
    {
        var options = new HealthEndpointOptions();
        configure?.Invoke(options);

        var config = new HealthConfig();
        configuration.GetSection("HealthCheck").Bind(config);

        var routeBuilder = app as IEndpointRouteBuilder ?? throw new InvalidOperationException("App must be an IEndpointRouteBuilder");

        // Liveness - simple text
        routeBuilder.MapGet("/health/live", async context =>
        {
            context.Response.ContentType = MediaTypeNames.Text.Plain;
            await context.Response.WriteAsync("Healthy");
        });

        // Readiness - filter by critical tag
        routeBuilder.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready") || r.Tags.Contains("critical"),
            ResponseWriter = async (ctx, report) =>
            {
                if (!ctx.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.ContentType = MediaTypeNames.Text.Plain;
                    await ctx.Response.WriteAsync(report.Status.ToString());
                    return;
                }

                ctx.Response.ContentType = MediaTypeNames.Application.Json;
                await WriteDetailsJsonAsync(ctx, report, serviceName: configuration["Service:Name"] ?? "Service");
            }
        });

        // Startup - optional
        if (config.EnableStartupProbe)
        {
            routeBuilder.MapHealthChecks("/health/startup", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("startup"),
            });
        }

        // Details - protected
        routeBuilder.MapGet("/health/details", async context =>
        {
            if (options.ProtectDetailsWithApiKey && config.DetailsEndpointAuth.Enabled)
            {
                if (!context.Request.Headers.TryGetValue("X-Health-ApiKey", out var key) || string.IsNullOrWhiteSpace(config.DetailsEndpointAuth.ApiKey) || key != config.DetailsEndpointAuth.ApiKey)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }

            var healthService = context.RequestServices.GetRequiredService<HealthCheckService>();
            var report = await healthService.CheckHealthAsync();
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await WriteDetailsJsonAsync(context, report, serviceName: configuration["Service:Name"] ?? "Service");
        });

        return app;
    }

    private static async Task WriteDetailsJsonAsync(HttpContext ctx, HealthReport report, string serviceName)
    {
        var entries = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            tags = e.Value.Tags,
            durationMs = (int)e.Value.Duration.TotalMilliseconds,
            error = e.Value.Exception?.Message ?? e.Value.Description
        }).ToArray();

        var payload = new
        {
            data = new
            {
                status = report.Status.ToString(),
                entries,
                durationMs = (int)report.TotalDuration.TotalMilliseconds,
                service = serviceName
            },
            errors = Array.Empty<object>(),
            warnings = Array.Empty<object>(),
            hasError = report.Status == HealthStatus.Unhealthy,
            hasWarning = report.Status == HealthStatus.Degraded,
            _links = new
            {
                self = new { href = "/health/details" },
                live = new { href = "/health/live" },
                ready = new { href = "/health/ready" },
            }
        };

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}