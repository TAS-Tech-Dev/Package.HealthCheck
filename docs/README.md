# Package.HealthCheck — Guia de Implementação

## Objetivo

Fornecer um pacote padronizado para verificação de vida (liveness), prontidão (readiness) e inicialização (startup) de serviços do MegaWish, além de checagens de dependências (DB, fila, cache, HTTP/grpc externos) e exposição de endpoints consistentes para orquestradores (Kubernetes, Docker Compose), BFFs e monitoramento.

## Escopo

- Extensões de registrar e expor health checks.
- Conjunto de IHealthCheck customizados para dependências comuns.
- Endpoints padronizados:
  - `GET /health/live` (liveness)
  - `GET /health/ready` (readiness)
  - `GET /health/startup` (startup – opcional)
  - `GET /health/details` (JSON detalhado, protegido)
- Tags/Severidade (critical | noncritical) e grupos (infra|external|internal).
- Integração com Observabilidade: métricas Prometheus, logs estruturados, traços (OTel), e eventos de mudança de estado.
- Publicação opcional do estado em mensageria (ex.: RabbitMQ) para um painel consolidado.
- Compatível com Kubernetes probes e Docker healthcheck.

---

## Estrutura do Pacote

- Nome: `Package.HealthCheck`
- Namespaces:
  - `Package.HealthCheck`
  - `Package.HealthCheck.Core`
  - `Package.HealthCheck.Checks`
  - `Package.HealthCheck.Endpoints`
  - `Package.HealthCheck.Integration`

Dependências (NuGet):

- `Microsoft.Extensions.Diagnostics.HealthChecks` (via framework)
- Provedores opcionais:
  - `AspNetCore.HealthChecks.NpgSql` / `SqlServer` / `MySql` / `MongoDb`
  - `AspNetCore.HealthChecks.Redis`
  - `AspNetCore.HealthChecks.RabbitMQ`
  - `AspNetCore.HealthChecks.Uris`
  - `OpenTelemetry.Extensions.Hosting`
  - `prometheus-net.AspNetCore`
  - `Serilog.AspNetCore`

---

## Modelo de Configuração

`appsettings.json` (exemplo):

```json
{
  "HealthCheck": {
    "EnableStartupProbe": true,
    "DetailsEndpointAuth": {
      "Enabled": true,
      "ApiKey": "super-secret-key"
    },
    "PublishToMessageBus": {
      "Enabled": false,
      "Broker": "amqp://guest:guest@rabbit:5672",
      "Exchange": "platform.health",
      "RoutingKey": "service.status"
    },
    "Dependencies": {
      "Postgres": {
        "ConnectionString": "Host=...;Username=...;Password=...;Database=..."
      },
      "Redis": {
        "ConnectionString": "redis:6379,password=..."
      },
      "RabbitMq": {
        "ConnectionString": "amqp://guest:guest@rabbit:5672"
      },
      "HttpDependencies": [
        { "Name": "Payments.API", "Url": "https://payments/api/ping", "Critical": true, "TimeoutSeconds": 2 },
        { "Name": "Geo.API", "Url": "https://geo/api/health", "Critical": false, "TimeoutSeconds": 2 }
      ]
    }
  }
}
```

---

## Convenções de Tags e Severidades

- Tags por finalidade: `["live"]`, `["ready"]`, `["startup"]`
- Tags por domínio: `["infra"]`, `["external"]`, `["internal"]`
- Severidade: `critical` (quebra readiness) vs `noncritical` (degrada, mas não derruba readiness)
- Política:
  - `live`: deve retornar Healthy se o processo está respondendo (não checa dependências).
  - `ready`: retorna Healthy somente se serviços critical estiverem saudáveis.
  - `startup`: Deve retornar Healthy somente após bootstrap (migrations, caches quentes, warm-up).

---

## API/Endpoints

### 1) GET /health/live
- Uso: Kubernetes livenessProbe, Docker.
- Resposta: 200 OK (Healthy) | 503 Service Unavailable (Unhealthy).
- Body (texto): `Healthy`/`Unhealthy`.

### 2) GET /health/ready
- Uso: Kubernetes readinessProbe.
- Considera only critical checks (DB, fila, cache, etc.).
- Resposta: `200 | 503`, com resumo em JSON quando `Accept: application/json`.

### 3) GET /health/startup
- Uso: startupProbe.
- Só fica Healthy após warm-ups concluídos.

### 4) GET /health/details
- Uso: Humanos/Observabilidade; protegido por API Key ou rede interna.
- Resposta (JSON) no padrão `data/errors/_links`.

---

## Registro no Program.cs

```csharp
using Package.HealthCheck;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMegaWishHealthChecks(builder.Configuration, options =>
{
    options.ServiceName = "Service.MS";
    options.EnableStartupProbe = true;

    // Dependências (critical)
    options.UsePostgres("postgres", builder.Configuration["HealthCheck:Dependencies:Postgres:ConnectionString"], critical: true);
    options.UseRedis("redis", builder.Configuration["HealthCheck:Dependencies:Redis:ConnectionString"], critical: true);
    options.UseRabbitMq("rabbitmq", builder.Configuration["HealthCheck:Dependencies:RabbitMq:ConnectionString"], critical: true);

    // HTTP externos
    options.UseHttpDependency("payments.api",
        url: builder.Configuration["HealthCheck:Dependencies:HttpDependencies:0:Url"],
        critical: true, timeoutSeconds: 2, tags: new[] { "external" });

    options.UseHttpDependency("geo.api",
        url: builder.Configuration["HealthCheck:Dependencies:HttpDependencies:1:Url"],
        critical: false, timeoutSeconds: 2, tags: new[] { "external" });

    // Checks de sistema (noncritical)
    options.UseDiskSpace(minimumFreeMb: 200, tagGroup: "infra");
    options.UseWorkingSet(maxMb: 1024, tagGroup: "infra");
});

var app = builder.Build();

app.UseMegaWishHealthEndpoints(builder.Configuration, opt =>
{
    opt.ProtectDetailsWithApiKey = true; // usa HealthCheck:DetailsEndpointAuth
});

app.MapControllers();
app.Run();
```

---

## Extensões Principais

- `IServiceCollection.AddMegaWishHealthChecks(IConfiguration, Action<MegaWishHealthOptions>)`
  - Configura registradores de checks comuns e permite `UseXyz(...)` na lambda.
  - Faz wire-up com OTel.
  - Registra background worker para métricas e publicação opcional (RabbitMQ).

- `IApplicationBuilder.UseMegaWishHealthEndpoints(IConfiguration, Action<HealthEndpointOptions>?)`
  - Mapeia `/health/live`, `/health/ready`, `/health/startup`, `/health/details`.
  - Aplica autenticação por API Key em `/health/details` quando habilitado.

### Checks inclusos

- Infra: Postgres/Redis/RabbitMQ, Disco, Memória.
- Rede: HTTP(s) ping com timeout.
- Custom: `StartupGateHealthCheck` (Unhealthy até bootstrap concluído).

---

## Política de Status

- Healthy: tudo OK ou apenas problemas noncritical.
- Degraded: ao menos um noncritical está ruim (readiness ainda pode ser 200).
- Unhealthy: qualquer critical falhou (readiness 503).

---

## Segurança do /health/details

- API Key via Header: `X-Health-ApiKey: <key>`
- Alternativa: Allowlist de IPs internos (implementar no host, se desejado).

---

## Integração com Observabilidade

### Métricas (Prometheus)

- `health_status{service="<svc>", check="<name>"} = 1|0|-1`
  - `1`: Healthy, `0`: Degraded, `-1`: Unhealthy
- `health_last_change_timestamp_seconds{service="<svc>"}`

### Logs

- Evento de mudança: `HealthStateChanged` com `Service`, `OldStatus`, `NewStatus`.

### Tracing (OTel)

- Span de avaliação de saúde pode ser adicionado no host; o pacote expõe integração básica via `AddOpenTelemetry()`.

---

## Publicação em Mensageria (Opcional)

Quando `PublishToMessageBus.Enabled = true`, o pacote publica mensagens (RabbitMQ):

```json
{
  "service": "Service.MS",
  "status": "Unhealthy",
  "timestamp": "2025-08-12T01:23:45Z",
  "entries": [
    { "name": "postgres", "status": "Healthy" },
    { "name": "payments.api", "status": "Unhealthy", "error": "Timeout" }
  ]
}
```

- Exchange: `platform.health` | routing key: `service.status`.

---

## Kubernetes — Probes (exemplo)

```yaml
livenessProbe:
  httpGet: { path: /health/live, port: 8080 }
  initialDelaySeconds: 20
  periodSeconds: 10
readinessProbe:
  httpGet: { path: /health/ready, port: 8080 }
  initialDelaySeconds: 25
  periodSeconds: 10
startupProbe:
  httpGet: { path: /health/startup, port: 8080 }
  failureThreshold: 30
  periodSeconds: 5
```

## Docker Compose — Healthcheck (exemplo)

```yaml
services:
  service.ms:
    image: megawish/service.ms:latest
    ports: ["8080:8080"]
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
      interval: 10s
      timeout: 2s
      retries: 10
      start_period: 30s
```

---

## Implementações Customizadas (exemplos)

### 1) StartupGateHealthCheck

```csharp
public sealed class StartupSignal
{
    public bool IsReady { get; private set; }
    public void MarkReady() => IsReady = true;
}

public sealed class StartupGateHealthCheck : IHealthCheck
{
    private readonly StartupSignal _signal;
    public StartupGateHealthCheck(StartupSignal signal) => _signal = signal;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        => Task.FromResult(_signal.IsReady
           ? HealthCheckResult.Healthy("Startup complete")
           : HealthCheckResult.Unhealthy("Startup in progress"));
}
```

No bootstrap:

```csharp
// Após migrations & warm-ups
app.Services.GetRequiredService<StartupSignal>().MarkReady();
```

### 2) HTTP Dependency Check com Timeout

```csharp
public sealed class HttpDependencyHealthCheck : IHealthCheck
{
    private readonly HttpClient _client;
    private readonly string _url;
    private readonly TimeSpan _timeout;

    public HttpDependencyHealthCheck(HttpClient client, string url, int timeoutSeconds = 2)
    { _client = client; _url = url; _timeout = TimeSpan.FromSeconds(timeoutSeconds); }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        using var cts = new CancellationTokenSource(_timeout);
        try
        {
            var res = await _client.GetAsync(_url, cts.Token);
            return res.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"HTTP {(int)res.StatusCode}");
        }
        catch (Exception e)
        { return HealthCheckResult.Unhealthy(e.Message); }
    }
}
```

---

## Passo a Passo de Adoção

1. Adicionar pacote `Package.HealthCheck` ao seu serviço.
2. Configurar `HealthCheck` no `appsettings.json`.
3. Registrar no `Program.cs` com `AddMegaWishHealthChecks(...)` e `UseMegaWishHealthEndpoints(...)`.
4. Configurar Probes (K8s) ou healthcheck (Compose).
5. Habilitar Observabilidade (métricas/logs/OTel) no host.
6. (Opcional) Publicação para painel via RabbitMQ.

---

## Roadmap

- Checks de ElasticSearch e S3.
- UI leve embutida em `/health/ui` (ambientes internos).
- Integração com Polly (circuit breaker awareness).
- Auto-discovery de checks por DI com `IHealthContributor`.