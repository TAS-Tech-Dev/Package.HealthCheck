using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Package.HealthCheck.Checks;

public sealed class WorkingSetHealthCheck : IHealthCheck
{
    private readonly int _maxMb;
    public WorkingSetHealthCheck(int maxMb) => _maxMb = maxMb;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var currentMb = Process.GetCurrentProcess().WorkingSet64 / 1024d / 1024d;
        if (currentMb > _maxMb)
        {
            return Task.FromResult(HealthCheckResult.Degraded($"WorkingSet {currentMb:F0}MB > {_maxMb}MB"));
        }
        return Task.FromResult(HealthCheckResult.Healthy($"WorkingSet {currentMb:F0}MB"));
    }
}