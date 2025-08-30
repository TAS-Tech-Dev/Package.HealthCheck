# 🚀 API Fluente para Health Checks

## Visão Geral

A nova API fluente permite configurar health checks de forma segura e intuitiva, mantendo dados sensíveis (como connection strings) no código em vez de arquivos de configuração.

## 🔐 Por que usar a API Fluente?

- **Segurança**: Dados sensíveis ficam no código, não em arquivos de configuração
- **IntelliSense**: Autocompletar e validação em tempo de compilação
- **Flexibilidade**: Configuração programática com fallback para configuração via arquivo
- **Manutenibilidade**: Código mais legível e fácil de manter

## 📝 Exemplos de Uso

### 1. Configuração Básica

```csharp
// Program.cs ou Startup.cs
builder.Services
    .AddHealthChecks("MeuServico")
    .AddPostgres("Server=localhost;Database=meudb;User Id=sa;Password=minhasenha;")
    .AddRedis("localhost:6379")
    .AddDashboard()
    .Build();
```

### 2. Configuração Avançada com Service Mesh

```csharp
builder.Services
    .AddHealthChecks("ServicoAvancado")
    .AddPostgres("Server=prod-db;Database=app;User Id=appuser;Password=senhasegura;")
    .AddRedis("prod-redis:6379,password=senharedis")
    .AddRabbitMq("amqp://user:senha@prod-rabbit:5672/")
    .AddServiceMesh("http://istio:15020", "Istio", "app-service", apiKey: "chave-api-istio")
    .AddPredictiveAnalysis(analysisWindowHours: 48, degradationThreshold: 0.25)
    .AddDashboard("/health-ui", enableAutoRefresh: true, refreshIntervalSeconds: 15)
    .EnableAutoDiscovery()
    .Build();
```

### 3. Dependências HTTP Seguras

```csharp
builder.Services
    .AddHealthChecks("ApiService")
    .AddHttpDependency("external-api", "https://api.externa.com/health", critical: true, timeoutSeconds: 5)
    .AddHttpDependency("internal-service", "http://servico-interno:8080/health", critical: false, timeoutSeconds: 2)
    .AddHttpDependency("third-party", "https://terceiro.com/status", critical: true, timeoutSeconds: 10, tags: new[] { "external", "payment" })
    .Build();
```

### 4. Connection Strings Customizadas

```csharp
builder.Services
    .AddHealthChecks("MultiDatabaseService")
    .AddCustomConnectionString("sqlserver", "Server=sqlserver;Database=app;User Id=user;Password=pass;", "sqlserver-health")
    .AddCustomConnectionString("mongodb", "mongodb://user:pass@mongodb:27017/app", "mongodb-health")
    .AddCustomConnectionString("mysql", "Server=mysql;Database=app;Uid=user;Pwd=pass;", "mysql-health")
    .Build();
```

### 5. Configuração Híbrida (YAML + Código)

```csharp
// appsettings.yaml - Configurações não sensíveis
HealthCheck:
  Dashboard:
    Enabled: true
    Route: "/health-dashboard"
    EnableAutoRefresh: true
  PredictiveAnalysis:
    Enabled: true
    AnalysisWindowHours: 24

// Program.cs - Dados sensíveis via código
builder.Services
    .AddHealthChecks("ServicoHibrido")
    .AddPostgres(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"))
    .AddRedis(Environment.GetEnvironmentVariable("REDIS_CONNECTION"))
    .AddServiceMesh(Environment.GetEnvironmentVariable("ISTIO_URL"), apiKey: Environment.GetEnvironmentVariable("ISTIO_API_KEY"))
    .Build();
```

## 🏗️ Estrutura da API

### HealthCheckBuilder

```csharp
public sealed class HealthCheckBuilder
{
    // Métodos principais
    public HealthCheckBuilder AddPostgres(string connectionString, string name = "postgres", string[]? tags = null)
    public HealthCheckBuilder AddRedis(string connectionString, string name = "redis", string[]? tags = null)
    public HealthCheckBuilder AddRabbitMq(string connectionString, string name = "rabbitmq", string[]? tags = null)
    public HealthCheckBuilder AddHttpDependency(string name, string url, bool critical = true, int timeoutSeconds = 2, string[]? tags = null)
    public HealthCheckBuilder AddDashboard(string route = "/health-dashboard", bool enableAutoRefresh = true, int refreshIntervalSeconds = 30)
    public HealthCheckBuilder AddServiceMesh(string baseUrl, string meshType = "Istio", string serviceName = "unknown", int timeoutSeconds = 30, string? apiKey = null)
    public HealthCheckBuilder AddPredictiveAnalysis(int analysisWindowHours = 24, int analysisIntervalMinutes = 15, double degradationThreshold = 0.3, double criticalThreshold = 0.7)
    public HealthCheckBuilder AddCustomConnectionString(string name, string connectionString, string healthCheckName, string[]? tags = null)
    
    // Configurações
    public HealthCheckBuilder EnableAutoDiscovery()
    public HealthCheckBuilder EnableStartupProbe()
    public HealthCheckBuilder WithServiceName(string serviceName)
    
    // Finalização
    public IServiceCollection Build()
}
```

## 🔄 Migração da API Antiga

### Antes (API Legada)

```csharp
// ❌ Antigo - Dados sensíveis em configuração
builder.Services.AddMegaWishHealthChecks(configuration);

// appsettings.json
{
  "HealthCheck": {
    "Dependencies": {
      "Postgres": {
        "ConnectionString": "Server=localhost;Database=app;Password=senha;"
      }
    }
  }
}
```

### Depois (Nova API Fluente)

```csharp
// ✅ Novo - Dados sensíveis no código
builder.Services
    .AddHealthChecks("MeuServico")
    .AddPostgres("Server=localhost;Database=app;Password=senha;")
    .Build();
```

## 🚨 Boas Práticas

### 1. **Segurança**
- Nunca coloque connection strings em arquivos de configuração
- Use variáveis de ambiente para dados sensíveis
- Considere usar Azure Key Vault, AWS Secrets Manager, etc.

### 2. **Organização**
- Agrupe configurações relacionadas
- Use nomes descritivos para health checks
- Aplique tags consistentes

### 3. **Manutenção**
- Documente configurações complexas
- Use constantes para valores repetidos
- Mantenha configurações em um local centralizado

## 📚 Exemplos Completos

### Exemplo 1: Microserviço com Banco de Dados

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configuração de Health Checks
builder.Services
    .AddHealthChecks("UserService")
    .AddPostgres(
        Environment.GetEnvironmentVariable("POSTGRES_CONNECTION") 
        ?? throw new InvalidOperationException("POSTGRES_CONNECTION not configured"),
        name: "user-database",
        tags: new[] { "database", "critical", "ready" }
    )
    .AddRedis(
        Environment.GetEnvironmentVariable("REDIS_CONNECTION") 
        ?? throw new InvalidOperationException("REDIS_CONNECTION not configured"),
        name: "user-cache",
        tags: new[] { "cache", "critical", "ready" }
    )
    .AddHttpDependency(
        "notification-service",
        Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL") + "/health",
        critical: false,
        timeoutSeconds: 3,
        tags: new[] { "external", "notification" }
    )
    .AddDashboard("/health-ui")
    .EnableAutoDiscovery()
    .Build();

var app = builder.Build();

// Mapear endpoints de health
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("startup")
});

app.Run();
```

### Exemplo 2: API Gateway com Múltiplos Serviços

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Health Checks para API Gateway
builder.Services
    .AddHealthChecks("ApiGateway")
    .AddServiceMesh(
        Environment.GetEnvironmentVariable("ISTIO_URL"),
        "Istio",
        "api-gateway",
        apiKey: Environment.GetEnvironmentVariable("ISTIO_API_KEY")
    )
    .AddPredictiveAnalysis(
        analysisWindowHours: 48,
        degradationThreshold: 0.2,
        criticalThreshold: 0.8
    )
    .AddDashboard("/gateway-health", enableAutoRefresh: true, refreshIntervalSeconds: 10)
    .Build();

var app = builder.Build();

// Endpoints de health
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});

app.Run();
```

## 🔧 Configuração de Ambiente

### Desenvolvimento

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddHealthChecks("DevService")
        .AddPostgres("Server=localhost;Database=dev;User Id=dev;Password=dev;")
        .AddRedis("localhost:6379")
        .AddDashboard()
        .Build();
}
```

### Produção

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services
        .AddHealthChecks("ProdService")
        .AddPostgres(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"))
        .AddRedis(Environment.GetEnvironmentVariable("REDIS_CONNECTION"))
        .AddServiceMesh(Environment.GetEnvironmentVariable("ISTIO_URL"))
        .AddPredictiveAnalysis()
        .Build();
}
```

## 📊 Monitoramento e Observabilidade

A API fluente integra automaticamente com:

- **OpenTelemetry**: Tracing distribuído
- **Prometheus**: Métricas de health checks
- **Serilog**: Logging estruturado
- **Dashboard**: Interface web para monitoramento
- **Service Mesh**: Integração com Istio, Linkerd, Consul

## 🎯 Próximos Passos

1. **Migre** suas configurações existentes para a nova API
2. **Configure** health checks sensíveis via código
3. **Mantenha** configurações não sensíveis em YAML/JSON
4. **Teste** a nova funcionalidade
5. **Documente** suas configurações específicas

---

**Nota**: A API antiga (`AddMegaWishHealthChecks`) ainda é suportada para compatibilidade, mas está marcada como obsoleta. Recomendamos migrar para a nova API fluente.
