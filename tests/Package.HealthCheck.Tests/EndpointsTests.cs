using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.HealthCheck;
using Package.HealthCheck.Endpoints;
using Xunit;

namespace Package.HealthCheck.Tests;

public class EndpointsTests
{
    private TestServer CreateServer(Action<IConfigurationBuilder>? configureConfig = null)
    {
        var builder = new WebHostBuilder()
            .UseTestServer()
            .ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Service:Name"] = "Test.Service",
                    ["HealthCheck:EnableStartupProbe"] = "false",
                    ["HealthCheck:DetailsEndpointAuth:Enabled"] = "true",
                    ["HealthCheck:DetailsEndpointAuth:ApiKey"] = "k"
                });
                configureConfig?.Invoke(cfg);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddRouting();
                services.AddMegaWishHealthChecks(ctx.Configuration);
            })
            .Configure((ctx, app) =>
            {
                app.UseRouting();
                app.UseMegaWishHealthEndpoints(ctx.Configuration, o => o.ProtectDetailsWithApiKey = true);
            });
        return new TestServer(builder);
    }

    [Fact]
    public async Task Live_ShouldReturn200_Healthy()
    {
        using var server = CreateServer();
        var res = await server.CreateClient().GetAsync("/health/live");
        ((int)res.StatusCode).Should().Be(200);
        var text = await res.Content.ReadAsStringAsync();
        text.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Ready_ShouldReturn200_WhenNoCritical()
    {
        using var server = CreateServer();
        var res = await server.CreateClient().GetAsync("/health/ready");
        ((int)res.StatusCode).Should().Be(200);
    }

    [Fact]
    public async Task Ready_ShouldReturnJson_WhenAcceptsJson()
    {
        using var server = CreateServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var res = await client.GetAsync("/health/ready");
        ((int)res.StatusCode).Should().Be(200);
        var contentType = res.Content.Headers.ContentType?.MediaType;
        contentType.Should().Be("application/json");
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("\"data\"");
    }

    [Fact]
    public async Task Details_ShouldReturn401_WithoutApiKey()
    {
        using var server = CreateServer();
        var res = await server.CreateClient().GetAsync("/health/details");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Details_ShouldReturn200_WithApiKey()
    {
        using var server = CreateServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Health-ApiKey", "k");
        var res = await client.GetAsync("/health/details");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var contentType = res.Content.Headers.ContentType?.MediaType;
        contentType.Should().Be("application/json");
    }

    [Fact]
    public async Task Startup_ShouldBeMapped_WhenEnabled()
    {
        using var server = CreateServer(cfg =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HealthCheck:EnableStartupProbe"] = "true"
            });
        });
        var res = await server.CreateClient().GetAsync("/health/startup");
        // When mapped but gate not marked ready, pipeline still returns 200 for endpoint existence; health check status may be 503
        ((int)res.StatusCode).Should().BeOneOf(200, 503);
    }

    [Fact]
    public async Task Startup_ShouldReturnHealthy_AfterMarkReady()
    {
        using var server = CreateServer(cfg =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HealthCheck:EnableStartupProbe"] = "true"
            });
        });
        var client = server.CreateClient();

        var res1 = await client.GetAsync("/health/startup");
        ((int)res1.StatusCode).Should().BeOneOf(200, 503);

        var signal = server.Services.GetRequiredService<Package.HealthCheck.Checks.StartupSignal>();
        signal.MarkReady();

        var res2 = await client.GetAsync("/health/startup");
        ((int)res2.StatusCode).Should().Be(200);
    }
}