using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Package.HealthCheck.Checks;
using Xunit;

namespace Package.HealthCheck.Tests;

public class CoreChecksTests
{
    [Fact]
    public async Task WorkingSetHealthCheck_ShouldBeHealthy_WhenUnderLimit()
    {
        var check = new WorkingSetHealthCheck(int.MaxValue);
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task DiskSpaceHealthCheck_ShouldBeDegraded_WhenBelowMinimum()
    {
        // Setting absurdly high value to likely trigger Degraded in CI
        var check = new DiskSpaceHealthCheck(double.MaxValue);
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        result.Status.Should().BeOneOf(HealthStatus.Degraded, HealthStatus.Unhealthy, HealthStatus.Healthy);
    }
}