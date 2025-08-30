# API Reference

## Visão Geral

Esta documentação fornece uma referência completa da API pública do Package.HealthCheck, incluindo todas as classes, métodos e propriedades disponíveis para desenvolvedores.

## 📚 Namespaces

### Package.HealthCheck
- **ServiceCollectionExtensions**: Extensões para configuração de serviços
- **HealthEndpointMappings**: Mapeamento de endpoints HTTP

### Package.HealthCheck.Core
- **HealthOptions**: Modelos de configuração
- **MegaWishHealthOptions**: Opções de configuração via código

### Package.HealthCheck.Checks
- **StartupGate**: Controle de estado de inicialização
- **DiskSpaceHealthCheck**: Monitoramento de espaço em disco
- **WorkingSetHealthCheck**: Monitoramento de memória
- **HttpDependencyHealthCheck**: Verificação de dependências HTTP

### Package.HealthCheck.Integration
- **HealthBackgroundWorker**: Worker em background para monitoramento

## 🔧 ServiceCollectionExtensions

### AddMegaWishHealthChecks

```csharp
public static IServiceCollection AddMegaWishHealthChecks(
    this IServiceCollection services, 
    IConfiguration configuration, 
    Action<MegaWishHealthOptions>? configure = null)
```

**Descrição**: Configura o sistema de health checks com todas as dependências necessárias.

**Parâmetros**:
- `services`: Container de serviços
- `configuration`: Configuração da aplicação
- `configure`: Ação opcional para configuração via código

**Retorno**: `IServiceCollection` para method chaining

**Exemplo**:
```csharp
builder.Services.AddMegaWishHealthChecks(builder.Configuration, options =>
{
    options.ServiceName = "MyService";
    options.UsePostgres("main-db", connectionString);
});
```

## 🎛️ MegaWishHealthOptions

### Propriedades

#### ServiceName
```csharp
public string ServiceName { get; set; } = "Service";
```
**Descrição**: Nome do serviço para identificação em logs e métricas.

#### EnableStartupProbe
```csharp
public bool EnableStartupProbe { get; set; } = true;
```
**Descrição**: Habilita ou desabilita o probe de startup.

### Métodos de Configuração

#### UsePostgres
```csharp
public MegaWishHealthOptions UsePostgres(
    string name, 
    string connectionString, 
    bool critical = true, 
    string[]? tags = null)
```

**Descrição**: Adiciona um health check para PostgreSQL.

**Parâmetros**:
- `name`: Nome do health check
- `connectionString`: String de conexão
- `critical`: Se a falha afeta o readiness
- `tags`: Tags adicionais

**Retorno**: `MegaWishHealthOptions` para method chaining

**Exemplo**:
```csharp
options.UsePostgres("main-db", connectionString, critical: true, tags: new[] { "database" });
```

#### UseRedis
```csharp
public MegaWishHealthOptions UseRedis(
    string name, 
    string connectionString, 
    bool critical = true, 
    string[]? tags = null)
```

**Descrição**: Adiciona um health check para Redis.

**Parâmetros**: Mesmos que `UsePostgres`

**Exemplo**:
```csharp
options.UseRedis("cache", redisConnectionString, critical: true);
```

#### UseRabbitMq
```csharp
public MegaWishHealthOptions UseRabbitMq(
    string name, 
    string connectionString, 
    bool critical = true, 
    string[]? tags = null)
```

**Descrição**: Adiciona um health check para RabbitMQ.

**Parâmetros**: Mesmos que `UsePostgres`

**Exemplo**:
```csharp
options.UseRabbitMq("message-broker", rabbitConnectionString, critical: true);
```

#### UseHttpDependency
```csharp
public MegaWishHealthOptions UseHttpDependency(
    string name, 
    string url, 
    bool critical = true, 
    int timeoutSeconds = 2, 
    string[]? tags = null)
```

**Descrição**: Adiciona um health check para dependência HTTP.

**Parâmetros**:
- `name`: Nome do health check
- `url`: URL do endpoint
- `critical`: Se a falha afeta o readiness
- `timeoutSeconds`: Timeout em segundos
- `tags`: Tags adicionais

**Exemplo**:
```csharp
options.UseHttpDependency("payments", "https://payments/health", critical: true, timeoutSeconds: 3);
```

#### UseDiskSpace
```csharp
public MegaWishHealthOptions UseDiskSpace(
    double minimumFreeMb, 
    string? tagGroup = "infra")
```

**Descrição**: Adiciona um health check para monitoramento de espaço em disco.

**Parâmetros**:
- `minimumFreeMb`: Espaço mínimo livre em MB
- `tagGroup`: Grupo de tags (padrão: "infra")

**Exemplo**:
```csharp
options.UseDiskSpace(minimumFreeMb: 500, tagGroup: "storage");
```

#### UseWorkingSet
```csharp
public MegaWishHealthOptions UseWorkingSet(
    int maxMb, 
    string? tagGroup = "infra")
```

**Descrição**: Adiciona um health check para monitoramento de memória.

**Parâmetros**:
- `maxMb`: Uso máximo de memória em MB
- `tagGroup`: Grupo de tags (padrão: "infra")

**Exemplo**:
```csharp
options.UseWorkingSet(maxMb: 1024, tagGroup: "memory");
```

## 🌐 HealthEndpointMappings

### UseMegaWishHealthEndpoints

```csharp
public static IApplicationBuilder UseMegaWishHealthEndpoints(
    this IApplicationBuilder app, 
    IConfiguration configuration, 
    Action<HealthEndpointOptions>? configure = null)
```

**Descrição**: Mapeia os endpoints de health check na aplicação.

**Parâmetros**:
- `app`: Builder da aplicação
- `configuration`: Configuração da aplicação
- `configure`: Ação opcional para configuração dos endpoints

**Retorno**: `IApplicationBuilder` para method chaining

**Exemplo**:
```csharp
app.UseMegaWishHealthEndpoints(builder.Configuration, opt =>
{
    opt.ProtectDetailsWithApiKey = true;
});
```

## ⚙️ HealthEndpointOptions

### Propriedades

#### ProtectDetailsWithApiKey
```csharp
public bool ProtectDetailsWithApiKey { get; set; } = true;
```
**Descrição**: Habilita proteção por API key no endpoint de detalhes.

## 🔐 HealthDetailsAuthOptions

### Propriedades

#### Enabled
```csharp
public bool Enabled { get; set; } = false;
```
**Descrição**: Habilita autenticação por API key.

#### ApiKey
```csharp
public string? ApiKey { get; set; }
```
**Descrição**: Chave de API para autenticação.

## 📊 HealthConfig

### Propriedades

#### EnableStartupProbe
```csharp
public bool EnableStartupProbe { get; set; } = true;
```
**Descrição**: Habilita o probe de startup.

#### DetailsEndpointAuth
```csharp
public HealthDetailsAuthOptions DetailsEndpointAuth { get; set; } = new();
```
**Descrição**: Configuração de autenticação para endpoint de detalhes.

#### PublishToMessageBus
```csharp
public HealthPublishOptions PublishToMessageBus { get; set; } = new();
```
**Descrição**: Configuração para publicação em message broker.

#### Dependencies
```csharp
public DependenciesConfig Dependencies { get; set; } = new();
```
**Descrição**: Configuração de dependências.

## 🐰 HealthPublishOptions

### Propriedades

#### Enabled
```csharp
public bool Enabled { get; set; } = false;
```
**Descrição**: Habilita publicação em message broker.

#### Broker
```csharp
public string? Broker { get; set; }
```
**Descrição**: URI de conexão com o broker.

#### Exchange
```csharp
public string Exchange { get; set; } = "platform.health";
```
**Descrição**: Nome do exchange RabbitMQ.

#### RoutingKey
```csharp
public string RoutingKey { get; set; } = "service.status";
```
**Descrição**: Chave de roteamento para mensagens.

## 🔗 DependenciesConfig

### Propriedades

#### Postgres
```csharp
public ConnectionStringConfig Postgres { get; set; } = new();
```
**Descrição**: Configuração de PostgreSQL.

#### Redis
```csharp
public ConnectionStringConfig Redis { get; set; } = new();
```
**Descrição**: Configuração de Redis.

#### RabbitMq
```csharp
public ConnectionStringConfig RabbitMq { get; set; } = new();
```
**Descrição**: Configuração de RabbitMQ.

#### HttpDependencies
```csharp
public List<HttpDependencyConfig> HttpDependencies { get; set; } = new();
```
**Descrição**: Lista de dependências HTTP.

## 🌐 HttpDependencyConfig

### Propriedades

#### Name
```csharp
public string Name { get; set; } = string.Empty;
```
**Descrição**: Nome identificador da dependência.

#### Url
```csharp
public string Url { get; set; } = string.Empty;
```
**Descrição**: URL do endpoint de health check.

#### Critical
```csharp
public bool Critical { get; set; } = true;
```
**Descrição**: Se a falha afeta o readiness.

#### TimeoutSeconds
```csharp
public int TimeoutSeconds { get; set; } = 2;
```
**Descrição**: Timeout em segundos.

## 🔌 ConnectionStringConfig

### Propriedades

#### ConnectionString
```csharp
public string? ConnectionString { get; set; }
```
**Descrição**: String de conexão com o serviço.

## 🚀 StartupSignal

### Propriedades

#### IsReady
```csharp
public bool IsReady { get; private set; }
```
**Descrição**: Indica se o serviço completou a inicialização.

### Métodos

#### MarkReady
```csharp
public void MarkReady() => IsReady = true;
```
**Descrição**: Marca o serviço como pronto.

## 🔍 StartupGateHealthCheck

### Construtor
```csharp
public StartupGateHealthCheck(StartupSignal signal)
```

**Parâmetros**:
- `signal`: Sinalizador de estado de inicialização

### CheckHealthAsync
```csharp
public Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context, 
    CancellationToken cancellationToken = default)
```

**Descrição**: Executa o health check de startup.

**Retorno**: `Task<HealthCheckResult>` com o status de inicialização.

## 💾 DiskSpaceHealthCheck

### Construtor
```csharp
public DiskSpaceHealthCheck(double minimumFreeMb, string? rootPath = null)
```

**Parâmetros**:
- `minimumFreeMb`: Espaço mínimo livre em MB
- `rootPath`: Caminho raiz para verificação (opcional)

### CheckHealthAsync
```csharp
public Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context, 
    CancellationToken cancellationToken = default)
```

**Descrição**: Verifica o espaço livre em disco.

**Retorno**: `Task<HealthCheckResult>` com o status do espaço em disco.

## 🧠 WorkingSetHealthCheck

### Construtor
```csharp
public WorkingSetHealthCheck(int maxMb)
```

**Parâmetros**:
- `maxMb`: Uso máximo de memória em MB

### CheckHealthAsync
```csharp
public Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context, 
    CancellationToken cancellationToken = default)
```

**Descrição**: Verifica o uso de memória do processo.

**Retorno**: `Task<HealthCheckResult>` com o status da memória.

## 🌐 HttpDependencyHealthCheck

### Construtor
```csharp
public HttpDependencyHealthCheck(
    HttpClient httpClient, 
    string url, 
    int timeoutSeconds = 2)
```

**Parâmetros**:
- `httpClient`: Cliente HTTP configurado
- `url`: URL para verificação
- `timeoutSeconds`: Timeout em segundos

### CheckHealthAsync
```csharp
public Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context, 
    CancellationToken cancellationToken = default)
```

**Descrição**: Verifica a disponibilidade de uma dependência HTTP.

**Retorno**: `Task<HealthCheckResult>` com o status da dependência.

## 🔄 HealthBackgroundWorker

### Construtor
```csharp
public HealthBackgroundWorker(
    ILogger<HealthBackgroundWorker> logger,
    HealthCheckService healthCheckService,
    IConfiguration configuration)
```

**Parâmetros**:
- `logger`: Logger para eventos
- `healthCheckService`: Serviço de health checks
- `configuration`: Configuração da aplicação

### ExecuteAsync
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
```

**Descrição**: Executa o worker em background para monitoramento contínuo.

## 📊 Métricas Prometheus

### Health Status Gauge
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

### Health Last Change Timestamp
```csharp
private static readonly Gauge HealthLastChangeGauge = Metrics.CreateGauge(
    "health_last_change_timestamp_seconds",
    "Unix timestamp of last health state change",
    new GaugeConfiguration { LabelNames = new[] { "service" } });
```

## 🔧 Extensibilidade

### Health Checks Customizados

Para criar health checks customizados, implemente a interface `IHealthCheck`:

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Implementação customizada
        return Task.FromResult(HealthCheckResult.Healthy("Custom check passed"));
    }
}
```

### Registro Customizado

```csharp
options.UseCustomHealthCheck("custom", sp => new CustomHealthCheck());
```

### Método de Extensão

```csharp
public static MegaWishHealthOptions UseCustomHealthCheck(
    this MegaWishHealthOptions options, 
    string name, 
    Func<IServiceProvider, IHealthCheck> factory)
{
    options._registrations.Add((services, hc) =>
    {
        hc.Add(new HealthCheckRegistration(name, factory, HealthStatus.Unhealthy, new[] { "custom" }));
    });
    return options;
}
```

## 📝 Notas de Uso

### Ordem de Configuração

1. **Configuração via arquivo** (appsettings.json)
2. **Configuração via código** (tem precedência)
3. **Valores padrão** (última prioridade)

### Sistema de Tags

O sistema aplica automaticamente tags baseadas na configuração:
- **Grupo**: `infra`, `external`, `internal`
- **Severidade**: `critical`, `noncritical`
- **Readiness**: `ready` (apenas para checks críticos)

### Filtros de Endpoints

- **`/health/live`**: Sem filtro (sempre responde)
- **`/health/ready`**: Filtra por `["ready"]` ou `["critical"]`
- **`/health/startup`**: Filtra por `["startup"]`
- **`/health/details`**: Sem filtro (todos os checks)

### Tratamento de Erros

- **Healthy**: Todos os checks críticos passaram
- **Degraded**: Alguns checks não-críticos falharam
- **Unhealthy**: Pelo menos um check crítico falhou

Esta documentação fornece uma referência completa da API, permitindo que os desenvolvedores utilizem adequadamente todas as funcionalidades do sistema.
