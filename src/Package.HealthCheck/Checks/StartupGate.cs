using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Package.HealthCheck.Checks;

public sealed class StartupSignal
{
    public bool IsReady { get; private set; }
    public void MarkReady() => IsReady = true;
}

public sealed class StartupGateHealthCheck : IHealthCheck
{
    private readonly StartupSignal _signal;
    public StartupGateHealthCheck(StartupSignal signal) => _signal = signal;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(_signal.IsReady
            ? HealthCheckResult.Healthy("Startup complete")
            : HealthCheckResult.Unhealthy("Startup in progress"));
}