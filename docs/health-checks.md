# Health Checks

## Visão Geral

O Package.HealthCheck fornece uma ampla gama de health checks pré-implementados, cobrindo desde dependências de infraestrutura até métricas de sistema. Todos os health checks seguem o padrão `IHealthCheck` do .NET e são configuráveis via fluent API.

## 🔍 Tipos de Health Checks

### 1. Core Probes

#### Live Probe
- **Nome**: `live`
- **Tags**: `["live"]`
- **Status**: Sempre `Healthy`
- **Uso**: Kubernetes livenessProbe, Docker healthcheck
- **Descrição**: Verifica se o processo está respondendo, sem verificar dependências

```csharp
// Registrado automaticamente
hcBuilder.AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
```

#### Startup Probe
- **Nome**: `startup`
- **Tags**: `["startup"]`
- **Status**: Controlado por `StartupSignal`
- **Uso**: Kubernetes startupProbe
- **Descrição**: Verifica se o serviço completou a inicialização

```csharp
// Configuração
options.EnableStartupProbe = true;

// Uso no código
var startupSignal = app.Services.GetRequiredService<StartupSignal>();
// ... após migrations, warm-ups, etc.
startupSignal.MarkReady();
```

#### Ready Probe
- **Nome**: `ready`
- **Tags**: Filtra por `["ready"]` ou `["critical"]`
- **Status**: Agregado de todos os health checks críticos
- **Uso**: Kubernetes readinessProbe
- **Descrição**: Verifica se o serviço está pronto para receber tráfego

### 2. Infraestrutura

#### PostgreSQL Health Check
- **Dependência**: `AspNetCore.HealthChecks.NpgSql`
- **Verificação**: Conectividade e execução de query simples
- **Tags**: `["infra", "critical", "ready"]` (quando crítico)

```csharp
// Via configuração
options.UsePostgres("main-db", connectionString, critical: true);

// Via arquivo
{
  "HealthCheck": {
    "Dependencies": {
      "Postgres": {
        "ConnectionString": "Host=localhost;Database=mydb;Username=user;Password=pass"
      }
    }
  }
}
```

#### Redis Health Check
- **Dependência**: `AspNetCore.HealthChecks.Redis`
- **Verificação**: Conectividade e execução de comando PING
- **Tags**: `["infra", "critical", "ready"]` (quando crítico)

```csharp
options.UseRedis("cache", connectionString, critical: true);
```

#### RabbitMQ Health Check
- **Dependência**: `AspNetCore.HealthChecks.RabbitMQ`
- **Verificação**: Conectividade e criação de canal
- **Tags**: `["infra", "critical", "ready"]` (quando crítico)

```csharp
options.UseRabbitMq("message-broker", connectionString, critical: true);
```

### 3. Sistema

#### Disk Space Health Check
- **Verificação**: Espaço livre em disco
- **Configuração**: Limite mínimo em MB
- **Status**: `Degraded` quando abaixo do limite
- **Tags**: `["infra", "noncritical"]`

```csharp
options.UseDiskSpace(minimumFreeMb: 500, tagGroup: "infra");

// Com caminho específico
options.UseDiskSpace(1000, @"C:\");
```

**Implementação**:
```csharp
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
            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.IsReady && string.Equals(d.RootDirectory.FullName, _rootPath, StringComparison.OrdinalIgnoreCase))
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
```

#### Working Set Health Check
- **Verificação**: Uso de memória do processo atual
- **Configuração**: Limite máximo em MB
- **Status**: `Degraded` quando acima do limite
- **Tags**: `["infra", "noncritical"]`

```csharp
options.UseWorkingSet(maxMb: 1024, tagGroup: "infra");
```

**Implementação**:
```csharp
public sealed class WorkingSetHealthCheck : IHealthCheck
{
    private readonly int _maxMb;
    
    public WorkingSetHealthCheck(int maxMb) => _maxMb = maxMb;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var currentMb = Process.GetCurrentProcess().WorkingSet64 / 1024d / 1024d;
        if (currentMb > _maxMb)
        {
            return Task.FromResult(HealthCheckResult.Degraded($"WorkingSet {currentMb:F0}MB > {_maxMb}MB"));
        }
        return Task.FromResult(HealthCheckResult.Healthy($"WorkingSet {currentMb:F0}MB"));
    }
}
```

### 4. Dependências Externas

#### HTTP Dependency Health Check
- **Verificação**: Resposta HTTP de endpoint externo
- **Configuração**: URL, timeout, criticidade
- **Status**: Baseado no status HTTP e criticidade
- **Tags**: `["external", "critical|noncritical", "ready"]` (quando crítico)

```csharp
options.UseHttpDependency("payments", "https://payments/health", critical: true, timeoutSeconds: 3);

// Com tags customizadas
options.UseHttpDependency("geo", "https://geo/health", critical: false, timeoutSeconds: 2, tags: new[] { "geolocation" });
```

**Implementação**:
```csharp
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
```

## 🏷️ Sistema de Tags

### Tags Automáticas

O sistema aplica automaticamente tags baseadas na configuração:

```csharp
private static string[] BuildTags(string[]? tags, string group, bool critical, bool includeReady)
{
    var baseTags = new List<string> { group };
    
    if (critical) 
        baseTags.Add("critical");
    else 
        baseTags.Add("noncritical");
    
    if (includeReady) 
        baseTags.Add("ready");
    
    if (tags != null && tags.Length > 0) 
        baseTags.AddRange(tags);
    
    return baseTags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
```

### Categorização por Grupo

- **`infra`**: Dependências de infraestrutura (banco, cache, fila)
- **`external`**: Serviços externos (APIs HTTP)
- **`internal`**: Serviços internos (quando implementado)

### Severidade

- **`critical`**: Falha afeta o readiness do serviço
- **`noncritical`**: Falha degrada o serviço mas não afeta readiness

### Readiness

- **`ready`**: Aplicado automaticamente a health checks críticos
- **`live`**: Sempre presente para probe de liveness
- **`startup`**: Aplicado ao probe de startup

## 🔧 Health Checks Customizados

### Implementação Básica

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Lógica de verificação
            var isHealthy = CheckCustomLogic();
            
            if (isHealthy)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Custom check passed"));
            }
            
            return Task.FromResult(HealthCheckResult.Degraded("Custom check failed"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Custom check error", ex));
        }
    }
}
```

### Registro via Fluent API

```csharp
// Método de extensão
public static MegaWishHealthOptions UseCustomHealthCheck(
    this MegaWishHealthOptions options, 
    string name, 
    Func<IServiceProvider, IHealthCheck> factory,
    bool critical = false,
    string[]? tags = null)
{
    options._registrations.Add((services, hc) =>
    {
        var tagSet = BuildTags(tags, group: "custom", critical: critical, includeReady: critical);
        hc.Add(new HealthCheckRegistration(name, factory, 
            critical ? HealthStatus.Unhealthy : HealthStatus.Degraded, 
            tagSet));
    });
    return options;
}

// Uso
options.UseCustomHealthCheck("custom", sp => new CustomHealthCheck(), critical: false, tags: new[] { "business" });
```

### Health Check com Dependências

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IDbConnection connection, ILogger<DatabaseHealthCheck> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            if (result?.ToString() == "1")
            {
                return HealthCheckResult.Healthy("Database connection successful");
            }
            
            return HealthCheckResult.Unhealthy("Database query failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
```

## 📊 Status e Resultados

### HealthCheckResult

```csharp
// Healthy
HealthCheckResult.Healthy("Operation successful");

// Degraded
HealthCheckResult.Degraded("Performance degraded", new Exception("Timeout"));

// Unhealthy
HealthCheckResult.Unhealthy("Critical failure", new Exception("Connection failed"));
```

### Agregação de Status

- **Healthy**: Todos os health checks críticos passaram
- **Degraded**: Alguns health checks não-críticos falharam
- **Unhealthy**: Pelo menos um health check crítico falhou

### Filtros por Tag

```csharp
// Readiness: apenas critical + ready
Predicate = r => r.Tags.Contains("ready") || r.Tags.Contains("critical")

// Startup: apenas startup
Predicate = r => r.Tags.Contains("startup")

// Liveness: sempre responde
// Sem filtro - sempre retorna Healthy
```

## 🚀 Health Checks Avançados

### Health Check com Cache

```csharp
public class CachedHealthCheck : IHealthCheck
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);

    public CachedHealthCheck(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"health_check_{context.Registration.Name}";
        
        if (_cache.TryGetValue(cacheKey, out HealthCheckResult? cachedResult))
        {
            return cachedResult!;
        }

        var result = await PerformHealthCheck(context, cancellationToken);
        _cache.Set(cacheKey, result, _cacheDuration);
        
        return result;
    }

    private async Task<HealthCheckResult> PerformHealthCheck(HealthCheckContext context, CancellationToken cancellationToken)
    {
        // Implementação real do health check
        return HealthCheckResult.Healthy();
    }
}
```

### Health Check com Circuit Breaker

```csharp
public class CircuitBreakerHealthCheck : IHealthCheck
{
    private readonly CircuitBreakerPolicy _circuitBreaker;
    private readonly Func<Task<HealthCheckResult>> _healthCheckFunc;

    public CircuitBreakerHealthCheck(Func<Task<HealthCheckResult>> healthCheckFunc)
    {
        _healthCheckFunc = healthCheckFunc;
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(_healthCheckFunc);
        }
        catch (BrokenCircuitException)
        {
            return HealthCheckResult.Unhealthy("Circuit breaker is open");
        }
    }
}
```

## 🔮 Health Checks Futuros

### Discovery Automático

```csharp
// Atributo para auto-discovery
[HealthCheck("database", Critical = true, Tags = new[] { "infra" })]
public class AutoDiscoveredHealthCheck : IHealthCheck
{
    // Implementação
}

// Configuração para auto-discovery
{
  "HealthCheck": {
    "AutoDiscovery": {
      "Enabled": true,
      "Assemblies": ["MyApp.*"],
      "Patterns": ["*HealthCheck", "*Service"]
    }
  }
}
```

### Health Checks Baseados em Configuração

```json
{
  "HealthCheck": {
    "CustomChecks": [
      {
        "Name": "file-system",
        "Type": "FileSystemHealthCheck",
        "Parameters": {
          "Path": "/var/log",
          "MinFreeSpace": 1000
        },
        "Critical": false,
        "Tags": ["infra", "storage"]
      }
    ]
  }
}
```

Esta documentação fornece uma visão completa dos health checks disponíveis, permitindo que os desenvolvedores implementem e configurem verificações de saúde adequadas para suas aplicações.
