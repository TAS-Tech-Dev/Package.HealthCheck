using System.IO;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Package.HealthCheck.Checks;

public sealed class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly double _minimumFreeMb;
    private readonly string _rootPath;

    public DiskSpaceHealthCheck(double minimumFreeMb, string? rootPath = null)
    {
        _minimumFreeMb = minimumFreeMb;
        _rootPath = rootPath ?? Path.GetPathRoot(Environment.CurrentDirectory)!;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && string.Equals(d.RootDirectory.FullName, _rootPath, StringComparison.OrdinalIgnoreCase))
                       ?? DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);

            if (drive is null)
            {
                return Task.FromResult(HealthCheckResult.Healthy("No drive information available"));
            }

            var freeMb = drive.AvailableFreeSpace / 1024d / 1024d;
            if (freeMb < _minimumFreeMb)
            {
                return Task.FromResult(HealthCheckResult.Degraded($"Low disk space: {freeMb:F0}MB < {_minimumFreeMb:F0}MB"));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"Free: {freeMb:F0}MB"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(ex.Message));
        }
    }
}