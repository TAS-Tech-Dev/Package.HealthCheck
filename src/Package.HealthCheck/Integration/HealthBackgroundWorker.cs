using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Prometheus;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Package.HealthCheck.Core;

namespace Package.HealthCheck.Integration;

public sealed class HealthBackgroundWorker : BackgroundService
{
    private static readonly Gauge HealthStatusGauge = Metrics.CreateGauge(
        "health_status",
        "Health status per service and check: 1 Healthy, 0 Degraded, -1 Unhealthy",
        new GaugeConfiguration { LabelNames = new[] { "service", "check" } });

    private static readonly Gauge HealthLastChangeGauge = Metrics.CreateGauge(
        "health_last_change_timestamp_seconds",
        "Unix timestamp of last health state change",
        new GaugeConfiguration { LabelNames = new[] { "service" } });

    private readonly string _serviceName;
    private readonly ILogger<HealthBackgroundWorker> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly HealthConfig _config;

    private HealthStatus? _lastStatus;
    private IConnection? _rabbitConnection;
    private IModel? _rabbitChannel;

    public HealthBackgroundWorker(
        ILogger<HealthBackgroundWorker> logger,
        HealthCheckService healthCheckService,
        IConfiguration configuration)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _serviceName = configuration["Service:Name"] ?? "Service";
        _config = new HealthConfig();
        configuration.GetSection("HealthCheck").Bind(_config);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var report = await _healthCheckService.CheckHealthAsync(stoppingToken);
                var overall = report.Status;

                // Metrics per entry
                foreach (var (name, entry) in report.Entries)
                {
                    HealthStatusGauge.WithLabels(_serviceName, name).Set(ConvertStatus(entry.Status));
                }
                // Overall
                HealthStatusGauge.WithLabels(_serviceName, "overall").Set(ConvertStatus(overall));

                if (_lastStatus != overall)
                {
                    HealthLastChangeGauge.WithLabels(_serviceName).Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    _logger.LogInformation("HealthStateChanged Service={Service} Old={Old} New={New}", _serviceName, _lastStatus, overall);
                    await PublishIfEnabledAsync(report, stoppingToken);
                    _lastStatus = overall;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health background worker error");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task PublishIfEnabledAsync(HealthReport report, CancellationToken ct)
    {
        if (!_config.PublishToMessageBus.Enabled || string.IsNullOrWhiteSpace(_config.PublishToMessageBus.Broker))
        {
            return;
        }

        try
        {
            EnsureRabbit();
            var payload = new
            {
                service = _serviceName,
                status = report.Status.ToString(),
                timestamp = DateTimeOffset.UtcNow.ToString("O"),
                entries = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    error = e.Value.Exception?.Message ?? e.Value.Description
                })
            };
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            _rabbitChannel!.BasicPublish(_config.PublishToMessageBus.Exchange, _config.PublishToMessageBus.RoutingKey, basicProperties: null, body: bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish health state");
        }
    }

    private void EnsureRabbit()
    {
        if (_rabbitConnection != null && _rabbitConnection.IsOpen && _rabbitChannel != null && _rabbitChannel.IsOpen)
        {
            return;
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_config.PublishToMessageBus.Broker!)
        };
        _rabbitConnection = factory.CreateConnection();
        _rabbitChannel = _rabbitConnection.CreateModel();
        _rabbitChannel.ExchangeDeclare(_config.PublishToMessageBus.Exchange, type: "fanout", durable: true, autoDelete: false);
    }

    private static double ConvertStatus(HealthStatus s) => s switch
    {
        HealthStatus.Healthy => 1,
        HealthStatus.Degraded => 0,
        HealthStatus.Unhealthy => -1,
        _ => -1
    };

    public override void Dispose()
    {
        base.Dispose();
        try { _rabbitChannel?.Dispose(); } catch { }
        try { _rabbitConnection?.Dispose(); } catch { }
    }
}