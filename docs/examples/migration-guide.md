# 🔄 Guia de Migração: v1 → v2

## 📋 Visão Geral

Este guia ajuda você a migrar do `Package.HealthCheck` v1 (que usa `AddMegaWishHealthChecks`) para a v2 (que usa a nova API fluente).

## 🚨 Importante

- **A API antiga ainda funciona** mas está marcada como obsoleta
- **Migração gradual** é possível - você pode usar ambas simultaneamente
- **Dados sensíveis** agora devem ser configurados via código
- **Configurações não sensíveis** podem continuar em YAML/JSON

## 🔄 Comparação: Antes vs. Depois

### ❌ **Antes (v1) - Dados sensíveis em configuração**

```csharp
// Program.cs
builder.Services.AddMegaWishHealthChecks(builder.Configuration);

// appsettings.json - ❌ DADOS SENSÍVEIS EXPOSTOS
{
  "HealthCheck": {
    "Dependencies": {
      "Postgres": {
        "ConnectionString": "Server=localhost;Database=app;Password=minhasenha;"
      },
      "Redis": {
        "ConnectionString": "localhost:6379,password=senharedis"
      }
    }
  }
}
```

### ✅ **Depois (v2) - Dados sensíveis protegidos**

```csharp
// Program.cs - Dados sensíveis via código
builder.Services
    .AddMegaWishHealthChecksBuilder("MeuServico")
    .AddPostgres(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"))
    .AddRedis(Environment.GetEnvironmentVariable("REDIS_CONNECTION"))
    .Build();

// appsettings.yaml - Apenas configurações não sensíveis
HealthCheck:
  Dashboard:
    Enabled: true
    Route: "/health-dashboard"
  PredictiveAnalysis:
    Enabled: true
    AnalysisWindowHours: 24
```

## 📝 **Passo a Passo da Migração**

### **Passo 1: Atualizar Dependências**

```bash
# Atualizar para a versão mais recente
dotnet add package Package.HealthCheck --version 2.0.0
```

### **Passo 2: Mover Dados Sensíveis para Variáveis de Ambiente**

#### **Antes (appsettings.json)**
```json
{
  "HealthCheck": {
    "Dependencies": {
      "Postgres": {
        "ConnectionString": "Server=prod-db;Database=app;Password=senha;"
      },
      "Redis": {
        "ConnectionString": "prod-redis:6379,password=senha"
      }
    }
  }
}
```

#### **Depois (.env ou variáveis de ambiente)**
```bash
# .env
POSTGRES_CONNECTION=Server=prod-db;Database=app;Password=senha;
REDIS_CONNECTION=prod-redis:6379,password=senha
RABBITMQ_CONNECTION=amqp://user:senha@prod-rabbit:5672/
```

### **Passo 3: Atualizar Program.cs**

#### **Antes (v1)**
```csharp
var builder = WebApplication.CreateBuilder(args);

// ❌ Antigo - dados sensíveis em configuração
builder.Services.AddMegaWishHealthChecks(builder.Configuration);

var app = builder.Build();

// Endpoints automáticos
app.UseMegaWishHealthEndpoints(builder.Configuration);

app.Run();
```

#### **Depois (v2)**
```csharp
var builder = WebApplication.CreateBuilder(args);

// ✅ Novo - dados sensíveis via código
builder.Services
    .AddHealthChecks("MeuServico")
    .AddPostgres(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"))
    .AddRedis(Environment.GetEnvironmentVariable("REDIS_CONNECTION"))
    .AddRabbitMq(Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION"))
    .Build();

var app = builder.Build();

// Mapear endpoints manualmente
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();
```

### **Passo 4: Configurar Variáveis de Ambiente**

#### **Desenvolvimento (.env)**
```bash
# .env
POSTGRES_CONNECTION=Server=localhost;Database=dev;User Id=dev;Password=dev;
REDIS_CONNECTION=localhost:6379
RABBITMQ_CONNECTION=amqp://guest:guest@localhost:5672/
```

#### **Produção (Docker/Kubernetes)**
```yaml
# docker-compose.yml
environment:
  - POSTGRES_CONNECTION=Server=prod-db;Database=app;Password=${DB_PASSWORD}
  - REDIS_CONNECTION=prod-redis:6379,password=${REDIS_PASSWORD}
  - RABBITMQ_CONNECTION=amqp://user:${RABBITMQ_PASSWORD}@prod-rabbit:5672/
```

```yaml
# kubernetes-deployment.yaml
env:
- name: POSTGRES_CONNECTION
  valueFrom:
    secretKeyRef:
      name: db-secrets
      key: postgres-connection
- name: REDIS_CONNECTION
  valueFrom:
    secretKeyRef:
      name: db-secrets
      key: redis-connection
```

### **Passo 5: Migrar Configurações Não Sensíveis**

#### **Manter em appsettings.yaml**
```yaml
# appsettings.yaml - Configurações não sensíveis
HealthCheck:
  Dashboard:
    Enabled: true
    Route: "/health-dashboard"
    EnableAutoRefresh: true
    RefreshIntervalSeconds: 30
  
  PredictiveAnalysis:
    Enabled: true
    AnalysisWindowHours: 24
    AnalysisIntervalMinutes: 15
    DegradationThreshold: 0.3
    CriticalThreshold: 0.7
  
  ServiceMesh:
    Enabled: true
    MeshType: "Istio"
    TimeoutSeconds: 30
    ReportMetrics: true
```

## 🔄 **Migração Gradual**

### **Fase 1: Configuração Híbrida**
```csharp
// Usar ambas as APIs simultaneamente
builder.Services
    .AddHealthChecks("MeuServico")
    .AddPostgres(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"))
    .Build();

// Manter configurações antigas para funcionalidades não migradas
builder.Services.AddMegaWishHealthChecks(builder.Configuration);
```

### **Fase 2: Migração Completa**
```csharp
// Migrar tudo para a nova API
builder.Services
    .AddHealthChecks("MeuServico")
    .AddPostgres(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"))
    .AddRedis(Environment.GetEnvironmentVariable("REDIS_CONNECTION"))
    .AddRabbitMq(Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION"))
    .AddDashboard()
    .AddPredictiveAnalysis()
    .EnableAutoDiscovery()
    .Build();
```

## 🆕 **Novas Funcionalidades Disponíveis**

### **1. API Fluente**
```csharp
builder.Services
    .AddHealthChecks("Servico")
    .AddPostgres(connectionString)
    .AddRedis(connectionString)
    .AddDashboard()
    .AddServiceMesh(url, type)
    .AddPredictiveAnalysis()
    .Build();
```

### **2. Auto-Discovery**
```csharp
builder.Services
    .AddHealthChecks("Servico")
    .EnableAutoDiscovery()  // Descobre DbContexts, HttpClients, etc.
    .Build();
```

### **3. HealthCheckAttribute**
```csharp
[HealthCheck("user-db", HealthCheckType.Database, tags: new[] { "critical", "ready" })]
public class UserDbContext : DbContext
{
    // Auto-discovery criará health check automaticamente
}
```

### **4. Suporte YAML**
```yaml
# healthchecks.yaml
HealthCheck:
  Dashboard:
    Enabled: true
    Route: "/health-dashboard"
  PredictiveAnalysis:
    Enabled: true
    AnalysisWindowHours: 24
```

## 🚨 **Problemas Comuns e Soluções**

### **Problema 1: "Connection string not found"**
```bash
# ❌ Erro
System.InvalidOperationException: POSTGRES_CONNECTION not configured

# ✅ Solução
export POSTGRES_CONNECTION="Server=localhost;Database=app;Password=senha;"
```

### **Problema 2: Health checks não aparecem**
```csharp
// ❌ Falta chamar .Build()
builder.Services
    .AddHealthChecks("Servico")
    .AddPostgres(connectionString);
    // .Build() está faltando!

// ✅ Correto
builder.Services
    .AddHealthChecks("Servico")
    .AddPostgres(connectionString)
    .Build();
```

### **Problema 3: Endpoints não funcionam**
```csharp
// ❌ Endpoints automáticos não existem mais
app.UseMegaWishHealthEndpoints(configuration);

// ✅ Mapear endpoints manualmente
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
```

## 📊 **Checklist de Migração**

- [ ] Atualizar package para v2.0.0
- [ ] Mover connection strings para variáveis de ambiente
- [ ] Atualizar Program.cs para usar nova API
- [ ] Configurar variáveis de ambiente (.env, Docker, K8s)
- [ ] Migrar configurações não sensíveis para YAML
- [ ] Mapear endpoints de health manualmente
- [ ] Testar todos os health checks
- [ ] Remover uso da API antiga
- [ ] Atualizar documentação da equipe

## 🔧 **Ferramentas de Apoio**

### **1. Validação de Configuração**
```csharp
// Validar se todas as variáveis estão configuradas
var requiredEnvVars = new[]
{
    "POSTGRES_CONNECTION",
    "REDIS_CONNECTION",
    "RABBITMQ_CONNECTION"
};

foreach (var envVar in requiredEnvVars)
{
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
    {
        throw new InvalidOperationException($"{envVar} not configured");
    }
}
```

### **2. Configuração Condicional**
```csharp
var healthChecks = builder.Services.AddHealthChecks("Servico");

// Adicionar health checks condicionalmente
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")))
{
    healthChecks.AddPostgres(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"));
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REDIS_CONNECTION")))
{
    healthChecks.AddRedis(Environment.GetEnvironmentVariable("REDIS_CONNECTION"));
}

healthChecks.Build();
```

## 📚 **Recursos Adicionais**

- [**API Fluente**](fluent-api-usage.md) - Documentação completa da nova API
- [**Exemplos**](program-example.cs) - Exemplos práticos de uso
- [**Atributos**](../attributes.md) - Como usar HealthCheckAttribute
- [**YAML**](healthchecks.yaml) - Exemplos de configuração YAML

## 🆘 **Precisa de Ajuda?**

Se você encontrar problemas durante a migração:

1. **Verifique** se todas as variáveis de ambiente estão configuradas
2. **Confirme** que está chamando `.Build()` no final da configuração
3. **Teste** cada health check individualmente
4. **Abra uma issue** no GitHub com detalhes do erro
5. **Consulte** a documentação da nova API

---

**🎯 Lembre-se**: A migração é gradual e a API antiga continua funcionando. Tome seu tempo para migrar corretamente!
