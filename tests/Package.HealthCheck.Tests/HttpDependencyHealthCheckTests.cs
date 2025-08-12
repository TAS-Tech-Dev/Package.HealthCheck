using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Package.HealthCheck.Checks;
using Xunit;

namespace Package.HealthCheck.Tests;

public class HttpDependencyHealthCheckTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }

    [Fact]
    public async Task ShouldReturnHealthy_On2xx()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var check = new HttpDependencyHealthCheck(client, "http://unit-test");
        var res = await check.CheckHealthAsync(new HealthCheckContext());
        res.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task ShouldReturnUnhealthy_OnNonSuccess()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var client = new HttpClient(handler);
        var check = new HttpDependencyHealthCheck(client, "http://unit-test");
        var res = await check.CheckHealthAsync(new HealthCheckContext());
        res.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task ShouldReturnUnhealthy_OnException()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("boom"));
        var client = new HttpClient(handler);
        var check = new HttpDependencyHealthCheck(client, "http://unit-test");
        var res = await check.CheckHealthAsync(new HealthCheckContext());
        res.Status.Should().Be(HealthStatus.Unhealthy);
        res.Description.Should().Contain("boom");
    }
}