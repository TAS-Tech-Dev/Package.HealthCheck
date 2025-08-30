# Configuração

## Visão Geral

O Package.HealthCheck oferece múltiplas formas de configuração, desde configuração simples via arquivos até configuração avançada via código, proporcionando flexibilidade para diferentes cenários de uso.

## 📁 Configuração via Arquivo

### Estrutura do appsettings.json

```json
{
  "HealthCheck": {
    "EnableStartupProbe": true,
    "DetailsEndpointAuth": {
      "Enabled": true,
      "ApiKey": "your-secret-api-key"
    },
    "PublishToMessageBus": {
      "Enabled": false,
      "Broker": "amqp://guest:guest@localhost:5672",
      "Exchange": "platform.health",
      "RoutingKey": "service.status"
    },
    "Dependencies": {
      "Postgres": {
        "ConnectionString": "Host=localhost;Database=mydb;Username=user;Password=pass"
      },
      "Redis": {
        "ConnectionString": "localhost:6379,password=secret"
      },
      "RabbitMq": {
        "ConnectionString": "amqp://guest:guest@localhost:5672"
      },
      "HttpDependencies": [
        {
          "Name": "Payments.API",
          "Url": "https://payments-api.example.com/health",
          "Critical": true,
          "TimeoutSeconds": 3
        },
        {
          "Name": "Geo.API",
          "Url": "https://geo-api.example.com/health",
          "Critical": false,
          "TimeoutSeconds": 2
        }
      ]
    }
  }
}
```

### Seções de Configuração

#### 1. EnableStartupProbe
- **Tipo**: `boolean`
- **Padrão**: `true`
- **Descrição**: Habilita ou desabilita o probe de startup
- **Uso**: Controle do endpoint `/health/startup`

#### 2. DetailsEndpointAuth
- **Tipo**: `HealthDetailsAuthOptions`
- **Descrição**: Configuração de autenticação para o endpoint de detalhes

```json
{
  "DetailsEndpointAuth": {
    "Enabled": true,
    "ApiKey": "your-secret-key"
  }
}
```

- **Enabled**: Habilita autenticação por API Key
- **ApiKey**: Chave secreta para autenticação

#### 3. PublishToMessageBus
- **Tipo**: `HealthPublishOptions`
- **Descrição**: Configuração para publicação de mudanças de estado

```json
{
  "PublishToMessageBus": {
    "Enabled": true,
    "Broker": "amqp://guest:guest@localhost:5672",
    "Exchange": "platform.health",
    "RoutingKey": "service.status"
  }
}
```

- **Enabled**: Habilita publicação para message broker
- **Broker**: URI de conexão com o broker
- **Exchange**: Nome do exchange RabbitMQ
- **RoutingKey**: Chave de roteamento para mensagens

#### 4. Dependencies
- **Tipo**: `DependenciesConfig`
- **Descrição**: Configuração de dependências de infraestrutura

##### Postgres
```json
{
  "Postgres": {
    "ConnectionString": "Host=localhost;Database=mydb;Username=user;Password=pass"
  }
}
```

##### Redis
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=secret"
  }
}
```

##### RabbitMQ
```json
{
  "RabbitMq": {
    "ConnectionString": "amqp://guest:guest@localhost:5672"
  }
}
```

##### HttpDependencies
```json
{
  "HttpDependencies": [
    {
      "Name": "Service.Name",
      "Url": "https://service.example.com/health",
      "Critical": true,
      "TimeoutSeconds": 3
    }
  ]
}
```

- **Name**: Nome identificador da dependência
- **Url**: URL do endpoint de health check
- **Critical**: Se a falha afeta o readiness do serviço
- **TimeoutSeconds**: Timeout em segundos para a verificação

## ⚙️ Configuração via Código

### Fluent API com MegaWishHealthOptions

```csharp
builder.Services.AddMegaWishHealthChecks(builder.Configuration, options =>
{
    // Configuração básica
    options.ServiceName = "MyService";
    options.EnableStartupProbe = true;

    // Dependências de infraestrutura
    options.UsePostgres("main-db", connectionString, critical: true);
    options.UseRedis("cache", redisConnectionString, critical: true);
    options.UseRabbitMq("message-broker", rabbitConnectionString, critical: true);

    // Dependências HTTP
    options.UseHttpDependency("payments", "https://payments/health", critical: true, timeoutSeconds: 3);
    options.UseHttpDependency("geo", "https://geo/health", critical: false, timeoutSeconds: 2);

    // Health checks de sistema
    options.UseDiskSpace(minimumFreeMb: 500, tagGroup: "infra");
    options.UseWorkingSet(maxMb: 1024, tagGroup: "infra");
});
```

### Métodos de Configuração Disponíveis

#### 1. UsePostgres
```csharp
options.UsePostgres(string name, string connectionString, bool critical = true, string[]? tags = null)
```

- **name**: Nome do health check
- **connectionString**: String de conexão PostgreSQL
- **critical**: Se a falha afeta o readiness
- **tags**: Tags adicionais para categorização

#### 2. UseRedis
```csharp
options.UseRedis(string name, string connectionString, bool critical = true, string[]? tags = null)
```

#### 3. UseRabbitMq
```csharp
options.UseRabbitMq(string name, string connectionString, bool critical = true, string[]? tags = null)
```

#### 4. UseHttpDependency
```csharp
options.UseHttpDependency(string name, string url, bool critical = true, int timeoutSeconds = 2, string[]? tags = null)
```

#### 5. UseDiskSpace
```csharp
options.UseDiskSpace(double minimumFreeMb, string? tagGroup = "infra")
```

#### 6. UseWorkingSet
```csharp
options.UseWorkingSet(int maxMb, string? tagGroup = "infra")
```

## 🏷️ Sistema de Tags

### Tags Automáticas

O sistema aplica automaticamente tags baseadas na configuração:

- **Grupo**: `infra`, `external`, `internal`
- **Severidade**: `critical`, `noncritical`
- **Readiness**: `ready` (apenas para checks críticos)

### Tags Customizadas

```csharp
options.UsePostgres("db", connectionString, critical: true, tags: new[] { "database", "primary", "production" });
```

### Estrutura de Tags

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

## 🔄 Precedência de Configuração

### Ordem de Prioridade

1. **Configuração via código** (mais alta prioridade)
2. **Configuração via arquivo** (appsettings.json)
3. **Valores padrão** (mais baixa prioridade)

### Exemplo de Override

```csharp
// appsettings.json
{
  "HealthCheck": {
    "EnableStartupProbe": false
  }
}

// Program.cs
builder.Services.AddMegaWishHealthChecks(builder.Configuration, options =>
{
    options.EnableStartupProbe = true; // Override do arquivo
});
```

## 🌍 Configuração por Ambiente

### appsettings.Development.json
```json
{
  "HealthCheck": {
    "Dependencies": {
      "Postgres": {
        "ConnectionString": "Host=localhost;Database=mydb_dev;Username=dev;Password=dev"
      },
      "Redis": {
        "ConnectionString": "localhost:6379"
      }
    }
  }
}
```

### appsettings.Production.json
```json
{
  "HealthCheck": {
    "Dependencies": {
      "Postgres": {
        "ConnectionString": "Host=prod-db;Database=mydb_prod;Username=prod;Password=${DB_PASSWORD}"
      },
      "Redis": {
        "ConnectionString": "prod-redis:6379,password=${REDIS_PASSWORD}"
      }
    },
    "PublishToMessageBus": {
      "Enabled": true,
      "Broker": "amqp://${RABBIT_USER}:${RABBIT_PASS}@prod-rabbit:5672"
    }
  }
}
```

## 🔐 Configuração de Segurança

### Variáveis de Ambiente

```bash
# PostgreSQL
DB_PASSWORD=your-secure-password

# Redis
REDIS_PASSWORD=your-redis-password

# RabbitMQ
RABBIT_USER=health-user
RABBIT_PASS=health-password

# API Key para health details
HEALTH_API_KEY=your-secret-key
```

### Configuração com Secrets

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

## 📊 Configuração de Observabilidade

### OpenTelemetry
```csharp
// Configuração automática via AddMegaWishHealthChecks
services.AddOpenTelemetry()
    .WithTracing(b =>
    {
        b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(options.ServiceName));
        b.AddSource("Package.HealthCheck");
    });
```

### Prometheus
```csharp
// Métricas automáticas via HealthBackgroundWorker
private static readonly Gauge HealthStatusGauge = Metrics.CreateGauge(
    "health_status",
    "Health status per service and check: 1 Healthy, 0 Degraded, -1 Unhealthy",
    new GaugeConfiguration { LabelNames = new[] { "service", "check" } });
```

## 🧪 Configuração para Testes

### Testes Unitários
```csharp
[Fact]
public void ShouldRegisterHealthCheckService_AndApplyConfiguredDependencies()
{
    var settings = new Dictionary<string, string?>
    {
        ["Service:Name"] = "Unit.Service",
        ["HealthCheck:Dependencies:Postgres:ConnectionString"] = "Host=localhost;Username=x;Password=y;Database=z",
        ["HealthCheck:Dependencies:Redis:ConnectionString"] = "localhost:6379"
    };
    
    var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    var services = new ServiceCollection();
    
    services.AddMegaWishHealthChecks(config, opt =>
    {
        opt.ServiceName = "Unit.Service";
        opt.UseDiskSpace(1);
    });

    var provider = services.BuildServiceProvider();
    var hc = provider.GetService<HealthCheckService>();
    hc.Should().NotBeNull();
}
```

### Testes de Integração
```csharp
// Usar configuração real ou mock para dependências externas
builder.Services.AddMegaWishHealthChecks(builder.Configuration, options =>
{
    // Configuração específica para testes
    options.UseHttpDependency("test-api", "http://localhost:5001/health", critical: false);
});
```

## 🔧 Configuração Avançada

### Health Checks Customizados
```csharp
options.UseCustomHealthCheck("custom", sp => new CustomHealthCheck());

// Ou via método de extensão
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

### Configuração Condicional
```csharp
if (builder.Environment.IsDevelopment())
{
    options.UseDiskSpace(100); // Limite menor para desenvolvimento
}
else
{
    options.UseDiskSpace(1000); // Limite maior para produção
}
```

Esta documentação fornece uma visão completa das opções de configuração disponíveis, permitindo que os desenvolvedores configurem o sistema de acordo com suas necessidades específicas.
