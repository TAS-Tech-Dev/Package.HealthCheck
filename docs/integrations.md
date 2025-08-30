# Integrações

## Visão Geral

O Package.HealthCheck oferece integrações robustas com sistemas de observabilidade, monitoramento e mensageria, permitindo uma visão completa da saúde dos serviços em tempo real.

## 📊 OpenTelemetry

### Configuração Automática
O sistema configura automaticamente o OpenTelemetry para rastreamento de health checks:

```csharp
services.AddOpenTelemetry()
    .WithTracing(b =>
    {
        b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(options.ServiceName));
        b.AddSource("Package.HealthCheck");
    });
```

### Source de Tracing
- **Nome**: `Package.HealthCheck`
- **Escopo**: Avaliação de health checks
- **Benefícios**: Rastreamento de performance e debugging

### Configuração Customizada
```csharp
services.AddOpenTelemetry()
    .WithTracing(b =>
    {
        b.SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(options.ServiceName)
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("service.version", "1.0.0"),
                new("service.environment", builder.Environment.EnvironmentName)
            }));
        
        b.AddSource("Package.HealthCheck");
        b.AddJaegerExporter();
        b.AddConsoleExporter();
    });
```

## 📈 Prometheus

### Métricas Automáticas
O `HealthBackgroundWorker` coleta automaticamente métricas Prometheus:

#### Health Status Gauge
```csharp
private static readonly Gauge HealthStatusGauge = Metrics.CreateGauge(
    "health_status",
    "Health status per service and check: 1 Healthy, 0 Degraded, -1 Unhealthy",
    new GaugeConfiguration { LabelNames = new[] { "service", "check" } });
```

**Labels**:
- `service`: Nome do serviço
- `check`: Nome do health check

**Valores**:
- `1`: Healthy
- `0`: Degraded
- `-1`: Unhealthy

#### Health Last Change Timestamp
```csharp
private static readonly Gauge HealthLastChangeGauge = Metrics.CreateGauge(
    "health_last_change_timestamp_seconds",
    "Unix timestamp of last health state change",
    new GaugeConfiguration { LabelNames = new[] { "service" } });
```

### Exposição de Métricas
```csharp
// Program.cs
app.UseMetricServer(); // Endpoint /metrics
app.UseHttpMetrics();  // Métricas HTTP
```

### Configuração de Métricas
```csharp
// Configuração customizada
app.UseMetricServer(opt =>
{
    opt.Endpoint = "/metrics";
    opt.EnableDefaultMetrics = true;
});

app.UseHttpMetrics(opt =>
{
    opt.RequestDuration.Enabled = true;
    opt.RequestCount.Enabled = true;
    opt.ResponseSize.Enabled = true;
});
```

### Exemplo de Métricas
```prometheus
# HELP health_status Health status per service and check: 1 Healthy, 0 Degraded, -1 Unhealthy
# TYPE health_status gauge
health_status{service="MyService",check="postgres"} 1
health_status{service="MyService",check="redis"} 1
health_status{service="MyService",check="diskspace"} 0
health_status{service="MyService",check="overall"} 1

# HELP health_last_change_timestamp_seconds Unix timestamp of last health state change
# TYPE health_last_change_timestamp_seconds gauge
health_last_change_timestamp_seconds{service="MyService"} 1.704123456e+09
```

## 📝 Serilog

### Logging Estruturado
O sistema utiliza Serilog para logging estruturado de eventos de saúde:

#### Evento de Mudança de Estado
```csharp
_logger.LogInformation("HealthStateChanged Service={Service} Old={Old} New={New}", 
    _serviceName, _lastStatus, overall);
```

#### Formato de Log
```json
{
  "Timestamp": "2025-01-12T10:30:45.123Z",
  "Level": "Information",
  "MessageTemplate": "HealthStateChanged Service={Service} Old={Old} New={New}",
  "Properties": {
    "Service": "MyService",
    "Old": "Healthy",
    "New": "Unhealthy",
    "SourceContext": "Package.HealthCheck.Integration.HealthBackgroundWorker"
  }
}
```

### Configuração de Serilog
```csharp
// Program.cs
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/health-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        IndexFormat = $"health-logs-{context.HostingEnvironment.EnvironmentName}-{DateTime.UtcNow:yyyy-MM}",
        AutoRegisterTemplate = true,
        NumberOfReplicas = 1,
        NumberOfShards = 2
    }));
```

## 🐰 RabbitMQ

### Publicação de Mudanças de Estado
O sistema pode publicar automaticamente mudanças de estado para um message broker:

#### Configuração
```json
{
  "HealthCheck": {
    "PublishToMessageBus": {
      "Enabled": true,
      "Broker": "amqp://guest:guest@localhost:5672",
      "Exchange": "platform.health",
      "RoutingKey": "service.status"
    }
  }
}
```

#### Formato da Mensagem
```json
{
  "service": "MyService",
  "status": "Unhealthy",
  "timestamp": "2025-01-12T10:30:45.123Z",
  "entries": [
    {
      "name": "postgres",
      "status": "Healthy",
      "error": null
    },
    {
      "name": "redis",
      "status": "Unhealthy",
      "error": "Connection timeout"
    }
  ]
}
```

#### Implementação
```csharp
private async Task PublishIfEnabledAsync(HealthReport report, CancellationToken ct)
{
    if (!_config.PublishToMessageBus.Enabled || 
        string.IsNullOrWhiteSpace(_config.PublishToMessageBus.Broker))
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
        _rabbitChannel!.BasicPublish(
            _config.PublishToMessageBus.Exchange, 
            _config.PublishToMessageBus.RoutingKey, 
            basicProperties: null, 
            body: bytes);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to publish health state");
    }
}
```

### Gerenciamento de Conexão
```csharp
private void EnsureRabbit()
{
    if (_rabbitConnection != null && _rabbitConnection.IsOpen && 
        _rabbitChannel != null && _rabbitChannel.IsOpen)
    {
        return;
    }

    var factory = new ConnectionFactory
    {
        Uri = new Uri(_config.PublishToMessageBus.Broker!)
    };
    
    _rabbitConnection = factory.CreateConnection();
    _rabbitChannel = _rabbitConnection.CreateModel();
    _rabbitChannel.ExchangeDeclare(
        _config.PublishToMessageBus.Exchange, 
        type: "fanout", 
        durable: true, 
        autoDelete: false);
}
```

## 🔄 Background Worker

### Funcionalidades
O `HealthBackgroundWorker` executa continuamente para:

1. **Coleta de Métricas**: Atualiza métricas Prometheus a cada 15 segundos
2. **Detecção de Mudanças**: Identifica mudanças no estado de saúde
3. **Publicação**: Envia mudanças para sistemas externos
4. **Logging**: Registra eventos importantes

### Ciclo de Execução
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync(stoppingToken);
            var overall = report.Status;

            // Métricas por entry
            foreach (var (name, entry) in report.Entries)
            {
                HealthStatusGauge.WithLabels(_serviceName, name)
                    .Set(ConvertStatus(entry.Status));
            }
            
            // Métrica overall
            HealthStatusGauge.WithLabels(_serviceName, "overall")
                .Set(ConvertStatus(overall));

            if (_lastStatus != overall)
            {
                HealthLastChangeGauge.WithLabels(_serviceName)
                    .Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                
                _logger.LogInformation("HealthStateChanged Service={Service} Old={Old} New={New}", 
                    _serviceName, _lastStatus, overall);
                
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
```

### Conversão de Status
```csharp
private static double ConvertStatus(HealthStatus s) => s switch
{
    HealthStatus.Healthy => 1,
    HealthStatus.Degraded => 0,
    HealthStatus.Unhealthy => -1,
    _ => -1
};
```

## 🚀 Integrações Futuras

### 1. Elasticsearch
```csharp
// Health check para Elasticsearch
options.UseElasticsearch("elasticsearch", "http://localhost:9200", critical: false);

// Logging para Elasticsearch
.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
{
    IndexFormat = $"health-logs-{DateTime.UtcNow:yyyy-MM}",
    AutoRegisterTemplate = true
})
```

### 2. Grafana
```csharp
// Métricas customizadas para dashboards
private static readonly Counter HealthCheckExecutions = Metrics.CreateCounter(
    "health_check_executions_total",
    "Total number of health check executions",
    new CounterConfiguration { LabelNames = new[] { "service", "check", "status" } });

// Uso
HealthCheckExecutions.WithLabels(_serviceName, checkName, status.ToString()).Inc();
```

### 3. Jaeger
```csharp
// Tracing para Jaeger
b.AddJaegerExporter(opt =>
{
    opt.AgentHost = "localhost";
    opt.AgentPort = 6831;
    opt.ServiceName = options.ServiceName;
});
```

### 4. Zipkin
```csharp
// Tracing para Zipkin
b.AddZipkinExporter(opt =>
{
    opt.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
    opt.ServiceName = options.ServiceName;
});
```

## 🔧 Configuração de Integrações

### Configuração por Ambiente

#### Development
```json
{
  "HealthCheck": {
    "PublishToMessageBus": {
      "Enabled": false
    }
  },
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/health-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

#### Production
```json
{
  "HealthCheck": {
    "PublishToMessageBus": {
      "Enabled": true,
      "Broker": "amqp://${RABBIT_USER}:${RABBIT_PASS}@rabbitmq:5672"
    }
  },
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://elasticsearch:9200",
          "indexFormat": "health-logs-{0:yyyy-MM}"
        }
      }
    ]
  }
}
```

### Variáveis de Ambiente
```bash
# RabbitMQ
RABBIT_USER=health-user
RABBIT_PASS=health-password

# Elasticsearch
ELASTICSEARCH_URL=http://elasticsearch:9200

# Jaeger
JAEGER_AGENT_HOST=jaeger
JAEGER_AGENT_PORT=6831
```

## 📊 Monitoramento e Alertas

### Grafana Dashboard
```json
{
  "dashboard": {
    "title": "Service Health Overview",
    "panels": [
      {
        "title": "Health Status by Service",
        "type": "stat",
        "targets": [
          {
            "expr": "health_status{check=\"overall\"}",
            "legendFormat": "{{service}}"
          }
        ]
      },
      {
        "title": "Health Check Duration",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(health_check_duration_seconds_sum[5m])",
            "legendFormat": "{{service}} - {{check}}"
          }
        ]
      }
    ]
  }
}
```

### Alertas Prometheus
```yaml
groups:
  - name: health-checks
    rules:
      - alert: ServiceUnhealthy
        expr: health_status{check="overall"} == -1
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Service {{ $labels.service }} is unhealthy"
          description: "Service {{ $labels.service }} has been unhealthy for more than 1 minute"

      - alert: HealthCheckDegraded
        expr: health_status{check="overall"} == 0
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "Service {{ $labels.service }} is degraded"
          description: "Service {{ $labels.service }} has been degraded for more than 2 minutes"
```

Esta documentação fornece uma visão completa das integrações disponíveis, permitindo que os desenvolvedores configurem adequadamente o sistema de observabilidade e monitoramento.
