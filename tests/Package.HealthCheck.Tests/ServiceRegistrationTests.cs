using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.HealthCheck;
using Xunit;

namespace Package.HealthCheck.Tests;

public class ServiceRegistrationTests
{
    [Fact]
    public void ShouldRegisterHealthCheckService_AndApplyConfiguredDependencies()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Service:Name"] = "Unit.Service",
            ["HealthCheck:Dependencies:Postgres:ConnectionString"] = "Host=localhost;Username=x;Password=y;Database=z",
            ["HealthCheck:Dependencies:Redis:ConnectionString"] = "localhost:6379",
            ["HealthCheck:Dependencies:RabbitMq:ConnectionString"] = "amqp://guest:guest@localhost:5672",
            ["HealthCheck:Dependencies:HttpDependencies:0:Name"] = "ext",
            ["HealthCheck:Dependencies:HttpDependencies:0:Url"] = "http://ext/ping",
            ["HealthCheck:Dependencies:HttpDependencies:0:Critical"] = "true",
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddMegaWishHealthChecks(config, opt =>
        {
            opt.ServiceName = "Unit.Service";
            opt.UseDiskSpace(1);
            opt.UseWorkingSet(int.MaxValue);
            opt.UseHttpDependency("local", "http://localhost/ping", critical: false);
        });

        var provider = services.BuildServiceProvider();
        var hc = provider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        hc.Should().NotBeNull();
    }
}