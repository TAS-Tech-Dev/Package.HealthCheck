using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Package.HealthCheck.Checks;

namespace Package.HealthCheck.Core;

public sealed partial class MegaWishHealthOptions
{
    private readonly List<Action<IServiceCollection, IHealthChecksBuilder>> _registrations = new();

    public MegaWishHealthOptions UsePostgres(string name, string connectionString, bool critical = true, string[]? tags = null)
    {
        _registrations.Add((services, hc) =>
        {
            var tagSet = BuildTags(tags, group: "infra", critical: critical, includeReady: true);
            hc.AddNpgSql(connectionString, name: name, tags: tagSet);
        });
        return this;
    }

    public MegaWishHealthOptions UseRedis(string name, string connectionString, bool critical = true, string[]? tags = null)
    {
        _registrations.Add((services, hc) =>
        {
            var tagSet = BuildTags(tags, group: "infra", critical: critical, includeReady: true);
            hc.AddRedis(connectionString, name: name, tags: tagSet);
        });
        return this;
    }

    public MegaWishHealthOptions UseRabbitMq(string name, string connectionString, bool critical = true, string[]? tags = null)
    {
        _registrations.Add((services, hc) =>
        {
            var tagSet = BuildTags(tags, group: "infra", critical: critical, includeReady: true);
            hc.AddRabbitMQ(connectionString, name: name, tags: tagSet);
        });
        return this;
    }

    public MegaWishHealthOptions UseHttpDependency(string name, string url, bool critical = true, int timeoutSeconds = 2, string[]? tags = null)
    {
        _registrations.Add((services, hc) =>
        {
            services.AddHttpClient($"health-http-{name}")
                .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));

            var tagSet = BuildTags(tags, group: "external", critical: critical, includeReady: critical);
            hc.Add(new HealthCheckRegistration(
                name: name,
                factory: sp => new HttpDependencyHealthCheck(sp.GetRequiredService<IHttpClientFactory>().CreateClient($"health-http-{name}"), url, timeoutSeconds),
                failureStatus: critical ? HealthStatus.Unhealthy : HealthStatus.Degraded,
                tags: tagSet));
        });
        return this;
    }

    public MegaWishHealthOptions UseDiskSpace(double minimumFreeMb, string? tagGroup = "infra")
    {
        _registrations.Add((services, hc) =>
        {
            var tagSet = BuildTags(tags: null, group: tagGroup ?? "infra", critical: false, includeReady: false);
            hc.Add(new HealthCheckRegistration(
                name: "diskspace",
                factory: sp => new DiskSpaceHealthCheck(minimumFreeMb),
                failureStatus: HealthStatus.Degraded,
                tags: tagSet));
        });
        return this;
    }

    public MegaWishHealthOptions UseWorkingSet(int maxMb, string? tagGroup = "infra")
    {
        _registrations.Add((services, hc) =>
        {
            var tagSet = BuildTags(tags: null, group: tagGroup ?? "infra", critical: false, includeReady: false);
            hc.Add(new HealthCheckRegistration(
                name: "workingset",
                factory: sp => new WorkingSetHealthCheck(maxMb),
                failureStatus: HealthStatus.Degraded,
                tags: tagSet));
        });
        return this;
    }

    internal void ApplyRegistrations(IServiceCollection services, IHealthChecksBuilder healthChecksBuilder)
    {
        foreach (var reg in _registrations)
        {
            reg(services, healthChecksBuilder);
        }
    }

    private static string[] BuildTags(string[]? tags, string group, bool critical, bool includeReady)
    {
        var baseTags = new List<string> { group };
        if (critical) baseTags.Add("critical"); else baseTags.Add("noncritical");
        if (includeReady) baseTags.Add("ready");
        if (tags != null && tags.Length > 0) baseTags.AddRange(tags);
        return baseTags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}