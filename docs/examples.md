# Exemplos de Uso

## Visão Geral

Esta documentação fornece exemplos práticos e completos de como utilizar o Package.HealthCheck em diferentes cenários, desde configurações básicas até implementações avançadas.

## 🚀 Configuração Básica

### 1. Configuração Mínima

#### Program.cs
```csharp
using Package.HealthCheck;

var builder = WebApplication.CreateBuilder(args);

// Configuração básica
builder.Services.AddMegaWishHealthChecks(builder.Configuration);

var app = builder.Build();

// Endpoints automáticos
app.UseMegaWishHealthEndpoints(builder.Configuration);

app.MapControllers();
app.Run();
```

#### appsettings.json
```json
{
  "HealthCheck": {
    "EnableStartupProbe": true
  }
}
```

**Resultado**: Endpoints básicos disponíveis em `/health/live`, `/health/ready`, `/health/startup`.

### 2. Configuração com Dependências

#### Program.cs
```csharp
builder.Services.AddMegaWishHealthChecks(builder.Configuration, options =>
{
    options.ServiceName = "UserService";
    
    // Dependências críticas
    options.UsePostgres("main-db", connectionString, critical: true);
    options.UseRedis("cache", redisConnectionString, critical: true);
    
    // Health checks de sistema
    options.UseDiskSpace(minimumFreeMb: 500);
    options.UseWorkingSet(maxMb: 1024);
});
```

#### appsettings.json
```json
{
  "HealthCheck": {
    "EnableStartupProbe": true,
    "Dependencies": {
      "Postgres": {
        "ConnectionString": "Host=localhost;Database=users;Username=user;Password=pass"
      },
      "Redis": {
        "ConnectionString": "localhost:6379"
      }
    }
  }
}
```

## 🏗️ Configurações Avançadas

### 1. Microserviço Completo

#### Program.cs
```csharp
using Package.HealthCheck;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços
builder.Services.AddDbContext<UserDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentService:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddRedis(builder.Configuration.GetConnectionString("Redis"));

// Health checks
builder.Services.AddMegaWishHealthChecks(builder.Configuration, options =>
{
    options.ServiceName = "UserService";
    options.EnableStartupProbe = true;
    
    // Dependências de infraestrutura
    options.UsePostgres("user-db", 
        builder.Configuration.GetConnectionString("DefaultConnection"), 
        critical: true);
    
    options.UseRedis("cache", 
        builder.Configuration.GetConnectionString("Redis"), 
        critical: true);
    
    // Dependências externas
    options.UseHttpDependency("payments", 
        $"{builder.Configuration["PaymentService:BaseUrl"]}/health", 
        critical: true, 
        timeoutSeconds: 5);
    
    // Health checks de sistema
    options.UseDiskSpace(minimumFreeMb: 1000, tagGroup: "storage");
    options.UseWorkingSet(maxMb: 2048, tagGroup: "memory");
});

var app = builder.Build();

// Startup sequence
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
await dbContext.Database.MigrateAsync();

// Marcar como pronto após migrations
var startupSignal = app.Services.GetRequiredService<StartupSignal>();
startupSignal.MarkReady();

// Endpoints de health
app.UseMegaWishHealthEndpoints(builder.Configuration, opt =>
{
    opt.ProtectDetailsWithApiKey = true;
});

app.MapControllers();
app.Run();
```

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=users;Username=user;Password=pass",
    "Redis": "localhost:6379"
  },
  "PaymentService": {
    "BaseUrl": "https://payments-api.company.com"
  },
  "HealthCheck": {
    "EnableStartupProbe": true,
    "DetailsEndpointAuth": {
      "Enabled": true,
      "ApiKey": "your-secret-api-key"
    },
    "PublishToMessageBus": {
      "Enabled": true,
      "Broker": "amqp://guest:guest@localhost:5672",
      "Exchange": "platform.health",
      "RoutingKey": "service.status"
    }
  }
}
```

### 2. Configuração por Ambiente

#### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=users_dev;Username=dev;Password=dev",
    "Redis": "localhost:6379"
  },
  "PaymentService": {
    "BaseUrl": "https://payments-dev.company.com"
  },
  "HealthCheck": {
    "PublishToMessageBus": {
      "Enabled": false
    }
  }
}
```

#### appsettings.Production.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=${DB_HOST};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}",
    "Redis": "${REDIS_HOST}:${REDIS_PORT},password=${REDIS_PASSWORD}"
  },
  "PaymentService": {
    "BaseUrl": "https://payments.company.com"
  },
  "HealthCheck": {
    "DetailsEndpointAuth": {
      "Enabled": true,
      "ApiKey": "${HEALTH_API_KEY}"
    },
    "PublishToMessageBus": {
      "Enabled": true,
      "Broker": "amqp://${RABBIT_USER}:${RABBIT_PASS}@${RABBIT_HOST}:5672"
    }
  }
}
```

#### Variáveis de Ambiente
```bash
# Database
DB_HOST=prod-db.company.com
DB_NAME=users_prod
DB_USER=prod_user
DB_PASSWORD=secure_password

# Redis
REDIS_HOST=prod-redis.company.com
REDIS_PORT=6379
REDIS_PASSWORD=redis_password

# RabbitMQ
RABBIT_USER=health_user
RABBIT_PASS=health_password
RABBIT_HOST=prod-rabbit.company.com

# Health Check
HEALTH_API_KEY=super_secret_key
```

## 🔧 Health Checks Customizados

### 1. Health Check de Negócio

#### BusinessHealthCheck.cs
```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UserService.HealthChecks;

public class BusinessHealthCheck : IHealthCheck
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<BusinessHealthCheck> _logger;

    public BusinessHealthCheck(IUserRepository userRepository, ILogger<BusinessHealthCheck> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verificar se há usuários ativos
            var activeUsersCount = await _userRepository.GetActiveUsersCountAsync(cancellationToken);
            
            if (activeUsersCount == 0)
            {
                return HealthCheckResult.Degraded("No active users found");
            }
            
            if (activeUsersCount < 100)
            {
                return HealthCheckResult.Degraded($"Low number of active users: {activeUsersCount}");
            }
            
            return HealthCheckResult.Healthy($"Active users: {activeUsersCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Business health check failed");
            return HealthCheckResult.Unhealthy("Business health check failed", ex);
        }
    }
}
```

#### Registro no Program.cs
```csharp
builder.Services.AddMegaWishHealthChecks(builder.Configuration, options =>
{
    // ... outras configurações ...
    
    // Health check customizado
    options.UseCustomHealthCheck("business", sp => 
        new BusinessHealthCheck(
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<ILogger<BusinessHealthCheck>>()
        ));
});
```

### 2. Health Check com Cache

#### CachedHealthCheck.cs
```csharp
public class CachedHealthCheck : IHealthCheck
{
    private readonly IHealthCheck _innerCheck;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration;

    public CachedHealthCheck(IHealthCheck innerCheck, IMemoryCache cache, TimeSpan cacheDuration)
    {
        _innerCheck = innerCheck;
        _cache = cache;
        _cacheDuration = cacheDuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"health_check_{context.Registration.Name}";
        
        if (_cache.TryGetValue(cacheKey, out HealthCheckResult? cachedResult))
        {
            return cachedResult!;
        }

        var result = await _innerCheck.CheckHealthAsync(context, cancellationToken);
        _cache.Set(cacheKey, result, _cacheDuration);
        
        return result;
    }
}
```

#### Uso
```csharp
options.UseCustomHealthCheck("cached-business", sp => 
    new CachedHealthCheck(
        new BusinessHealthCheck(
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<ILogger<BusinessHealthCheck>>()
        ),
        sp.GetRequiredService<IMemoryCache>(),
        TimeSpan.FromSeconds(30)
    ));
```

### 3. Health Check Condicional

#### ConditionalHealthCheck.cs
```csharp
public class ConditionalHealthCheck : IHealthCheck
{
    private readonly IHealthCheck _innerCheck;
    private readonly Func<bool> _condition;
    private readonly string _conditionName;

    public ConditionalHealthCheck(IHealthCheck innerCheck, Func<bool> condition, string conditionName)
    {
        _innerCheck = innerCheck;
        _condition = condition;
        _conditionName = conditionName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_condition())
        {
            return HealthCheckResult.Healthy($"Health check condition '{_conditionName}' not met");
        }

        return await _innerCheck.CheckHealthAsync(context, cancellationToken);
    }
}
```

#### Uso
```csharp
options.UseCustomHealthCheck("conditional-business", sp => 
    new ConditionalHealthCheck(
        new BusinessHealthCheck(
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<ILogger<BusinessHealthCheck>>()
        ),
        () => builder.Environment.IsProduction(), // Só executa em produção
        "Production Environment"
    ));
```

## 🐳 Docker e Kubernetes

### 1. Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["UserService.csproj", "./"]
RUN dotnet restore "UserService.csproj"
COPY . .
RUN dotnet build "UserService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.dll"]
```

### 2. Docker Compose
```yaml
version: '3.8'

services:
  user-service:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=users;Username=user;Password=pass
      - ConnectionStrings__Redis=redis:6379
      - PaymentService__BaseUrl=https://payments-dev.company.com
    depends_on:
      - postgres
      - redis
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
      interval: 10s
      timeout: 2s
      retries: 10
      start_period: 30s

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: users
      POSTGRES_USER: user
      POSTGRES_PASSWORD: pass
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U user -d users"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5
```

### 3. Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: user-service
  labels:
    app: user-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: user-service
  template:
    metadata:
      labels:
        app: user-service
    spec:
      containers:
      - name: user-service
        image: company/user-service:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        - name: ConnectionStrings__Redis
          valueFrom:
            secretKeyRef:
              name: redis-secret
              key: connection-string
        - name: PaymentService__BaseUrl
          value: "https://payments.company.com"
        - name: HealthCheck__DetailsEndpointAuth__ApiKey
          valueFrom:
            secretKeyRef:
              name: health-secret
              key: api-key
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
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: user-service
spec:
  selector:
    app: user-service
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
---
apiVersion: v1
kind: Secret
metadata:
  name: db-secret
type: Opaque
data:
  connection-string: <base64-encoded-connection-string>
---
apiVersion: v1
kind: Secret
metadata:
  name: redis-secret
type: Opaque
data:
  connection-string: <base64-encoded-redis-connection>
---
apiVersion: v1
kind: Secret
metadata:
  name: health-secret
type: Opaque
data:
  api-key: <base64-encoded-api-key>
```

## 📊 Monitoramento e Observabilidade

### 1. Configuração do Prometheus
```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'user-service'
    static_configs:
      - targets: ['user-service:8080']
    metrics_path: '/metrics'
    scrape_interval: 10s
```

### 2. Grafana Dashboard
```json
{
  "dashboard": {
    "title": "User Service Health",
    "panels": [
      {
        "title": "Health Status",
        "type": "stat",
        "targets": [
          {
            "expr": "health_status{service=\"UserService\",check=\"overall\"}",
            "legendFormat": "Overall Health"
          }
        ],
        "fieldConfig": {
          "defaults": {
            "color": {
              "mode": "thresholds"
            },
            "thresholds": {
              "steps": [
                {"color": "red", "value": null},
                {"color": "red", "value": -1},
                {"color": "yellow", "value": 0},
                {"color": "green", "value": 1}
              ]
            }
          }
        }
      },
      {
        "title": "Health Check Duration",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(health_check_duration_seconds_sum[5m])",
            "legendFormat": "{{check}}"
          }
        ]
      }
    ]
  }
}
```

### 3. Alertas Prometheus
```yaml
groups:
  - name: user-service-health
    rules:
      - alert: UserServiceUnhealthy
        expr: health_status{service="UserService",check="overall"} == -1
        for: 1m
        labels:
          severity: critical
          service: user-service
        annotations:
          summary: "User Service is unhealthy"
          description: "User Service has been unhealthy for more than 1 minute"
          
      - alert: UserServiceDegraded
        expr: health_status{service="UserService",check="overall"} == 0
        for: 2m
        labels:
          severity: warning
          service: user-service
        annotations:
          summary: "User Service is degraded"
          description: "User Service has been degraded for more than 2 minutes"
          
      - alert: DatabaseUnhealthy
        expr: health_status{service="UserService",check="main-db"} == -1
        for: 30s
        labels:
          severity: critical
          service: user-service
          component: database
        annotations:
          summary: "User Service database is unhealthy"
          description: "Database health check is failing"
```

## 🔍 Testes

### 1. Teste Unitário
```csharp
[Fact]
public void ShouldRegisterHealthCheckService_AndApplyConfiguredDependencies()
{
    // Arrange
    var settings = new Dictionary<string, string?>
    {
        ["Service:Name"] = "UserService",
        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=users;Username=user;Password=pass",
        ["ConnectionStrings:Redis"] = "localhost:6379",
        ["PaymentService:BaseUrl"] = "https://payments.company.com"
    };
    
    var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    var services = new ServiceCollection();
    
    // Act
    services.AddMegaWishHealthChecks(config, opt =>
    {
        opt.ServiceName = "UserService";
        opt.UsePostgres("main-db", config.GetConnectionString("DefaultConnection"), critical: true);
        opt.UseRedis("cache", config.GetConnectionString("Redis"), critical: true);
        opt.UseHttpDependency("payments", $"{config["PaymentService:BaseUrl"]}/health", critical: true);
        opt.UseDiskSpace(500);
        opt.UseWorkingSet(1024);
    });

    var provider = services.BuildServiceProvider();
    
    // Assert
    var healthCheckService = provider.GetService<HealthCheckService>();
    healthCheckService.Should().NotBeNull();
    
    var startupSignal = provider.GetService<StartupSignal>();
    startupSignal.Should().NotBeNull();
}
```

### 2. Teste de Integração
```csharp
[Fact]
public async Task ShouldReturnHealthy_WhenAllDependenciesAreHealthy()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/health/ready");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Be("Healthy");
}

[Fact]
public async Task ShouldReturnUnhealthy_WhenDatabaseIsDown()
{
    // Arrange
    // Simular falha no banco de dados
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/health/ready");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
}
```

## 🚀 Deploy e CI/CD

### 1. GitHub Actions
```yaml
name: Build and Deploy User Service

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Build Docker image
      run: docker build -t user-service:${{ github.sha }} .
    
    - name: Push to registry
      run: |
        echo ${{ secrets.DOCKER_PASSWORD }} | docker login -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
        docker tag user-service:${{ github.sha }} ${{ secrets.DOCKER_USERNAME }}/user-service:${{ github.sha }}
        docker push ${{ secrets.DOCKER_USERNAME }}/user-service:${{ github.sha }}
    
    - name: Deploy to staging
      if: github.ref == 'refs/heads/main'
      run: |
        kubectl set image deployment/user-service user-service=${{ secrets.DOCKER_USERNAME }}/user-service:${{ github.sha }} -n staging
```

### 2. Helm Chart
```yaml
# values.yaml
replicaCount: 3

image:
  repository: company/user-service
  tag: latest
  pullPolicy: IfNotPresent

service:
  type: ClusterIP
  port: 80
  targetPort: 8080

ingress:
  enabled: true
  className: nginx
  hosts:
    - host: user-service.company.com
      paths:
        - path: /
          pathType: Prefix

resources:
  requests:
    memory: 256Mi
    cpu: 250m
  limits:
    memory: 512Mi
    cpu: 500m

healthCheck:
  livenessProbe:
    initialDelaySeconds: 20
    periodSeconds: 10
  readinessProbe:
    initialDelaySeconds: 25
    periodSeconds: 10
  startupProbe:
    failureThreshold: 30
    periodSeconds: 5

env:
  - name: ASPNETCORE_ENVIRONMENT
    value: "Production"
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: db-secret
        key: connection-string
```

## 📋 Checklist de Implementação

### ✅ Configuração Básica
- [ ] Adicionar pacote `Package.HealthCheck`
- [ ] Configurar `AddMegaWishHealthChecks` no Program.cs
- [ ] Configurar `UseMegaWishHealthEndpoints`
- [ ] Testar endpoints básicos

### ✅ Dependências
- [ ] Configurar health checks para banco de dados
- [ ] Configurar health checks para cache
- [ ] Configurar health checks para filas
- [ ] Configurar health checks para APIs externas

### ✅ Sistema
- [ ] Configurar health checks de disco
- [ ] Configurar health checks de memória
- [ ] Configurar startup probe

### ✅ Segurança
- [ ] Configurar autenticação para endpoint de detalhes
- [ ] Configurar API key
- [ ] Testar acesso protegido

### ✅ Observabilidade
- [ ] Configurar OpenTelemetry
- [ ] Configurar Prometheus
- [ ] Configurar Serilog
- [ ] Configurar RabbitMQ (opcional)

### ✅ Deploy
- [ ] Configurar Docker
- [ ] Configurar Kubernetes
- [ ] Configurar health checks no deploy
- [ ] Configurar monitoramento

### ✅ Testes
- [ ] Testes unitários
- [ ] Testes de integração
- [ ] Testes de health checks
- [ ] Testes de endpoints

Esta documentação fornece exemplos práticos e completos para implementar o Package.HealthCheck em diferentes cenários, permitindo que os desenvolvedores comecem rapidamente e implementem funcionalidades avançadas conforme necessário.
