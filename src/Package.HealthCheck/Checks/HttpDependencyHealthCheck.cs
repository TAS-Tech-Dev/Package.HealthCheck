using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Package.HealthCheck.Checks;

public sealed class HttpDependencyHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly TimeSpan _timeout;

    public HttpDependencyHealthCheck(HttpClient httpClient, string url, int timeoutSeconds = 2)
    {
        _httpClient = httpClient;
        _url = url;
        _timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(_timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        try
        {
            var response = await _httpClient.GetAsync(_url, linkedCts.Token);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}