# Endpoints

## Visão Geral

O Package.HealthCheck expõe automaticamente um conjunto padronizado de endpoints HTTP para health checks, seguindo as melhores práticas para orquestradores como Kubernetes e Docker. Todos os endpoints são configuráveis e seguem padrões REST.

## 🌐 Endpoints Disponíveis

### 1. `/health/live` - Liveness Probe

#### Características
- **Método**: `GET`
- **Propósito**: Verificar se o processo está respondendo
- **Status**: Sempre retorna `200 OK`
- **Body**: Texto simples
- **Tags**: `["live"]`

#### Resposta
```http
HTTP/1.1 200 OK
Content-Type: text/plain

Healthy
```

#### Uso
- **Kubernetes**: `livenessProbe`
- **Docker**: `healthcheck`
- **Load Balancers**: Verificação de disponibilidade básica

#### Implementação
```csharp
routeBuilder.MapGet("/health/live", async context =>
{
    context.Response.ContentType = MediaTypeNames.Text.Plain;
    await context.Response.WriteAsync("Healthy");
});
```

### 2. `/health/ready` - Readiness Probe

#### Características
- **Método**: `GET`
- **Propósito**: Verificar se o serviço está pronto para receber tráfego
- **Status**: `200 OK` (Healthy) ou `503 Service Unavailable` (Unhealthy)
- **Body**: Texto simples ou JSON detalhado
- **Tags**: Filtra por `["ready"]` ou `["critical"]`

#### Resposta Simples
```http
HTTP/1.1 200 OK
Content-Type: text/plain

Healthy
```

#### Resposta JSON (quando `Accept: application/json`)
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "data": {
    "status": "Healthy",
    "entries": [
      {
        "name": "postgres",
        "status": "Healthy",
        "tags": ["infra", "critical", "ready"],
        "durationMs": 45,
        "error": null
      },
      {
        "name": "redis",
        "status": "Healthy",
        "tags": ["infra", "critical", "ready"],
        "durationMs": 12,
        "error": null
      }
    ],
    "durationMs": 67,
    "service": "MyService"
  },
  "errors": [],
  "warnings": [],
  "hasError": false,
  "hasWarning": false,
  "_links": {
    "self": { "href": "/health/ready" },
    "live": { "href": "/health/live" },
    "startup": { "href": "/health/startup" }
  }
}
```

#### Implementação
```csharp
routeBuilder.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready") || r.Tags.Contains("critical"),
    ResponseWriter = async (ctx, report) =>
    {
        if (!ctx.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.ContentType = MediaTypeNames.Text.Plain;
            await ctx.Response.WriteAsync(report.Status.ToString());
            return;
        }

        ctx.Response.ContentType = MediaTypeNames.Application.Json;
        await WriteDetailsJsonAsync(ctx, report, serviceName: configuration["Service:Name"] ?? "Service");
    }
});
```

### 3. `/health/startup` - Startup Probe

#### Características
- **Método**: `GET`
- **Propósito**: Verificar se o serviço completou a inicialização
- **Status**: `200 OK` (Healthy) ou `503 Service Unavailable` (Unhealthy)
- **Body**: Texto simples
- **Tags**: `["startup"]`
- **Configuração**: Opcional via `EnableStartupProbe`

#### Resposta
```http
HTTP/1.1 200 OK
Content-Type: text/plain

Healthy
```

#### Implementação
```csharp
if (config.EnableStartupProbe)
{
    routeBuilder.MapHealthChecks("/health/startup", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("startup"),
    });
}
```

### 4. `/health/details` - Detalhes Completos

#### Características
- **Método**: `GET`
- **Propósito**: Fornecer informações detalhadas sobre todos os health checks
- **Status**: `200 OK` (sempre)
- **Body**: JSON detalhado
- **Autenticação**: Protegido por API Key (opcional)
- **Tags**: Todos os health checks registrados

#### Autenticação
```http
GET /health/details HTTP/1.1
X-Health-ApiKey: your-secret-api-key
```

#### Resposta
```json
{
  "data": {
    "status": "Healthy",
    "entries": [
      {
        "name": "live",
        "status": "Healthy",
        "tags": ["live"],
        "durationMs": 0,
        "error": null
      },
      {
        "name": "startup",
        "status": "Healthy",
        "tags": ["startup"],
        "durationMs": 1,
        "error": null
      },
      {
        "name": "postgres",
        "status": "Healthy",
        "tags": ["infra", "critical", "ready"],
        "durationMs": 45,
        "error": null
      },
      {
        "name": "redis",
        "status": "Healthy",
        "tags": ["infra", "critical", "ready"],
        "durationMs": 12,
        "error": null
      },
      {
        "name": "diskspace",
        "status": "Healthy",
        "tags": ["infra", "noncritical"],
        "durationMs": 3,
        "error": null
      }
    ],
    "durationMs": 67,
    "service": "MyService"
  },
  "errors": [],
  "warnings": [],
  "hasError": false,
  "hasWarning": false,
  "_links": {
    "self": { "href": "/health/details" },
    "live": { "href": "/health/live" },
    "ready": { "href": "/health/ready" },
    "startup": { "href": "/health/startup" }
  }
}
```

#### Implementação
```csharp
routeBuilder.MapGet("/health/details", async context =>
{
    if (options.ProtectDetailsWithApiKey && config.DetailsEndpointAuth.Enabled)
    {
        if (!context.Request.Headers.TryGetValue("X-Health-ApiKey", out var key) || 
            string.IsNullOrWhiteSpace(config.DetailsEndpointAuth.ApiKey) || 
            key != config.DetailsEndpointAuth.ApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
    }

    var healthService = context.RequestServices.GetRequiredService<HealthCheckService>();
    var report = await healthService.CheckHealthAsync();
    context.Response.ContentType = MediaTypeNames.Application.Json;
    await WriteDetailsJsonAsync(context, report, serviceName: configuration["Service:Name"] ?? "Service");
});
```

## 🔧 Configuração dos Endpoints

### Registro Automático
```csharp
var app = builder.Build();

app.UseMegaWishHealthEndpoints(builder.Configuration, opt =>
{
    opt.ProtectDetailsWithApiKey = true;
});
```

### Opções de Configuração

#### HealthEndpointOptions
```csharp
public sealed class HealthEndpointOptions
{
    public bool ProtectDetailsWithApiKey { get; set; } = true;
}
```

#### HealthDetailsAuthOptions
```csharp
public sealed class HealthDetailsAuthOptions
{
    public bool Enabled { get; set; } = false;
    public string? ApiKey { get; set; }
}
```

## 🏷️ Sistema de Filtros por Tags

### Liveness Probe
- **Sem filtro**: Sempre executa todos os health checks
- **Resultado**: Sempre `Healthy` (endpoint básico)

### Readiness Probe
- **Filtro**: `r.Tags.Contains("ready") || r.Tags.Contains("critical")`
- **Propósito**: Apenas health checks que afetam o readiness
- **Resultado**: Agregado dos health checks críticos

### Startup Probe
- **Filtro**: `r.Tags.Contains("startup")`
- **Propósito**: Apenas health checks de inicialização
- **Resultado**: Status do processo de startup

### Details Endpoint
- **Sem filtro**: Todos os health checks registrados
- **Propósito**: Visão completa do sistema
- **Resultado**: Status detalhado de todos os componentes

## 📊 Formato de Resposta

### Estrutura JSON Padrão
```json
{
  "data": {
    "status": "string",           // Healthy, Degraded, Unhealthy
    "entries": [                  // Array de health checks
      {
        "name": "string",         // Nome do health check
        "status": "string",       // Status individual
        "tags": ["string"],       // Tags associadas
        "durationMs": "number",   // Duração em milissegundos
        "error": "string|null"    // Mensagem de erro (se houver)
      }
    ],
    "durationMs": "number",       // Duração total
    "service": "string"           // Nome do serviço
  },
  "errors": [],                   // Array de erros
  "warnings": [],                 // Array de warnings
  "hasError": "boolean",          // Se há erros
  "hasWarning": "boolean",        // Se há warnings
  "_links": {                     // Links relacionados
    "self": { "href": "string" },
    "live": { "href": "string" },
    "ready": { "href": "string" },
    "startup": { "href": "string" }
  }
}
```

### Implementação do Response Writer
```csharp
private static async Task WriteDetailsJsonAsync(HttpContext ctx, HealthReport report, string serviceName)
{
    var entries = report.Entries.Select(e => new
    {
        name = e.Key,
        status = e.Value.Status.ToString(),
        tags = e.Value.Tags,
        durationMs = (int)e.Value.Duration.TotalMilliseconds,
        error = e.Value.Exception?.Message ?? e.Value.Description
    }).ToArray();

    var payload = new
    {
        data = new
        {
            status = report.Status.ToString(),
            entries,
            durationMs = (int)report.TotalDuration.TotalMilliseconds,
            service = serviceName
        },
        errors = Array.Empty<object>(),
        warnings = Array.Empty<object>(),
        hasError = report.Status == HealthStatus.Unhealthy,
        hasWarning = report.Status == HealthStatus.Degraded,
        _links = new
        {
            self = new { href = "/health/details" },
            live = new { href = "/health/live" },
            ready = new { href = "/health/ready" },
        }
    };

    await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
```

## 🔒 Segurança

### Autenticação por API Key
- **Header**: `X-Health-ApiKey`
- **Configuração**: Via `HealthDetailsAuthOptions`
- **Endpoint**: Apenas `/health/details`
- **Fallback**: `401 Unauthorized`

### Configuração de Segurança
```json
{
  "HealthCheck": {
    "DetailsEndpointAuth": {
      "Enabled": true,
      "ApiKey": "your-secret-api-key"
    }
  }
}
```

### Variáveis de Ambiente
```bash
HEALTH_API_KEY=your-secret-key
```

```json
{
  "HealthCheck": {
    "DetailsEndpointAuth": {
      "Enabled": true,
      "ApiKey": "${HEALTH_API_KEY}"
    }
  }
}
```

## 🚀 Configuração Avançada

### Endpoints Customizados
```csharp
// Endpoint adicional para métricas específicas
routeBuilder.MapGet("/health/metrics", async context =>
{
    var metrics = await CollectCustomMetrics(context.RequestServices);
    context.Response.ContentType = MediaTypeNames.Application.Json;
    await context.Response.WriteAsync(JsonSerializer.Serialize(metrics));
});
```

### Middleware Customizado
```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health"))
    {
        // Logging customizado para health checks
        context.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogInformation("Health check request: {Path}", context.Request.Path);
    }
    
    await next();
});
```

## 📋 Exemplos de Uso

### Kubernetes
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 20
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 25
  periodSeconds: 10

startupProbe:
  httpGet:
    path: /health/startup
    port: 8080
  failureThreshold: 30
  periodSeconds: 5
```

### Docker Compose
```yaml
services:
  app:
    image: myapp:latest
    ports:
      - "8080:8080"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
      interval: 10s
      timeout: 2s
      retries: 10
      start_period: 30s
```

### Load Balancer
```nginx
upstream backend {
    server app1:8080 max_fails=3 fail_timeout=30s;
    server app2:8080 max_fails=3 fail_timeout=30s;
}

server {
    location /health {
        proxy_pass http://backend;
        proxy_next_upstream error timeout http_503;
    }
}
```

### Monitoramento
```bash
# Verificar liveness
curl -f http://localhost:8080/health/live

# Verificar readiness
curl -f http://localhost:8080/health/ready

# Verificar startup
curl -f http://localhost:8080/health/startup

# Ver detalhes (com autenticação)
curl -H "X-Health-ApiKey: your-key" http://localhost:8080/health/details
```

Esta documentação fornece uma visão completa dos endpoints disponíveis, permitindo que os desenvolvedores e operadores configurem e utilizem adequadamente o sistema de health checks.
