# 🏥 Package.HealthCheck

## 📋 Visão Geral

O **Package.HealthCheck** é uma solução abrangente e inteligente para monitoramento de saúde de aplicações .NET, oferecendo configuração plug-and-play, descoberta automática de dependências e análise preditiva com machine learning.

## 🎯 Objetivos

- **Configuração Plug-and-Play**: Descoberta automática de dependências e configuração inteligente
- **Segurança**: Dados sensíveis protegidos via código, configurações não sensíveis via YAML/JSON
- **Flexibilidade**: API fluente para configuração programática + suporte a arquivos de configuração
- **Inteligência**: Análise preditiva e auto-healing baseado em padrões históricos
- **Integração**: Service Mesh, OpenTelemetry, Prometheus, e mais
- **Observabilidade**: Dashboard integrado e métricas em tempo real

## 🏗️ Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                    Package.HealthCheck                      │
├─────────────────────────────────────────────────────────────┤
│  🔍 Auto-Discovery    │  🎛️  API Fluente    │  📄 YAML/JSON  │
│  • DbContexts         │  • AddPostgres()   │  • Config      │
│  • HttpClients        │  • AddRedis()      │  • Dashboard   │
│  • Services           │  • AddDashboard()  │  • ML Config   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Health Checks                           │
├─────────────────────────────────────────────────────────────┤
│  🗄️  Databases  │  🌐 HTTP      │  🧠 ML        │  🕸️  Mesh    │
│  • PostgreSQL   │  • Dependencies│  • Predictive │  • Istio    │
│  • Redis        │  • Timeouts    │  • Analysis   │  • Linkerd  │
│  • RabbitMQ     │  • Critical    │  • Auto-heal  │  • Consul   │
│  • SQL Server   │  • Tags        │  • Alerts     │             │
│  • MySQL        │                │  • History    │             │
│  • MongoDB      │                │               │             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Observabilidade                         │
├─────────────────────────────────────────────────────────────┤
│  📊 Dashboard  │  📈 Metrics   │  🔍 Tracing   │  📝 Logging  │
│  • Web UI      │  • Prometheus │  • OpenTelemetry│  • Serilog  │
│  • Real-time   │  • Custom     │  • Distributed │  • Structured│
│  • Auto-refresh│  • Health     │  • Correlation │  • JSON     │
└─────────────────────────────────────────────────────────────┘
```

## 🚀 Funcionalidades

### 🔍 **Auto-Discovery (v2.0)**
- Descoberta automática de `DbContext`s registrados
- Detecção de `HttpClient`s configurados
- Busca por serviços com `HealthCheckAttribute`
- Criação automática de health checks apropriados

### 🎛️ **API Fluente (v2.0)**
- Configuração programática para dados sensíveis
- Builder pattern intuitivo e type-safe
- IntelliSense completo e validação em tempo de compilação
- Exemplo: `services.AddHealthChecks("Service").AddPostgres(connString).AddDashboard()`

### 📄 **Suporte a YAML (v2.0)**
- Configuração via arquivos YAML (não sensíveis)
- Parsing customizado com `YamlDotNet`
- Integração com sistema de configuração do .NET
- Fallback para configurações padrão

### 🕸️ **Integração Service Mesh (v2.0)**
- Suporte a Istio, Linkerd e Consul
- Health checks de conectividade de mesh
- Métricas de latência e disponibilidade
- Configuração via API fluente ou YAML

### 🧠 **Análise Preditiva com ML (v2.0)**
- Análise de padrões históricos de health
- Detecção precoce de degradação
- Alertas proativos e auto-healing
- Configuração de thresholds personalizados

### 📊 **Dashboard Integrado (v2.0)**
- Interface web para monitoramento em tempo real
- Auto-refresh configurável
- Visualização de status e métricas
- Rota customizável

### 🔐 **Segurança Híbrida**
- **Dados sensíveis**: Configurados via código (connection strings, API keys)
- **Configurações não sensíveis**: Via YAML/JSON (timeouts, routes, thresholds)
- **Variáveis de ambiente**: Para configurações de produção
- **Azure Key Vault/AWS Secrets Manager**: Integração planejada

## 📝 Exemplos de Uso

### 🔐 **API Fluente (Recomendado para dados sensíveis)**

```csharp
// Program.cs
builder.Services
    .AddMegaWishHealthChecksBuilder("UserService")
    .AddPostgres(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"))
    .AddRedis(Environment.GetEnvironmentVariable("REDIS_CONNECTION"))
    .AddServiceMesh(Environment.GetEnvironmentVariable("ISTIO_URL"), apiKey: Environment.GetEnvironmentVariable("ISTIO_API_KEY"))
    .AddPredictiveAnalysis(analysisWindowHours: 48, degradationThreshold: 0.25)
    .AddDashboard("/health-ui", enableAutoRefresh: true, refreshIntervalSeconds: 15)
    .EnableAutoDiscovery()
    .Build();
```

### 📄 **Configuração YAML (Para configurações não sensíveis)**

```yaml
# healthchecks.yaml
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

### 🔄 **Abordagem Híbrida (Recomendada)**

```csharp
// Dados sensíveis via código
builder.Services
    .AddMegaWishHealthChecksBuilder("HybridService")
    .AddPostgres(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"))
    .AddRedis(Environment.GetEnvironmentVariable("REDIS_CONNECTION"))
    .Build();

// Configurações não sensíveis via YAML
// (carregadas automaticamente do appsettings.yaml)
```

## 🏷️ HealthCheckAttribute

Configure health checks de forma declarativa:

```csharp
[HealthCheck("user-database", HealthCheckType.Database, tags: new[] { "critical", "ready" })]
public class UserDbContext : DbContext
{
    // Implementação do contexto
}

[HealthCheck("external-api", HealthCheckType.Http, timeoutSeconds: 5, isCritical: true)]
public class ExternalApiService
{
    // Implementação do serviço
}
```

## 🔧 Configuração

### 1. **Instalação**

```bash
dotnet add package Package.HealthCheck
```

### 2. **Configuração Básica**

```csharp
// Program.cs
builder.Services.AddMegaWishHealthChecksBuilder("MeuServico");

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
```

### 3. **Configuração Avançada**

```csharp
builder.Services
    .AddMegaWishHealthChecksBuilder("ServicoAvancado")
    .AddPostgres("Server=localhost;Database=app;User Id=user;Password=pass;")
    .AddRedis("localhost:6379")
    .AddServiceMesh("http://istio:15020", "Istio", "app-service")
    .AddPredictiveAnalysis()
    .AddDashboard()
    .EnableAutoDiscovery()
    .Build();
```

## 📊 Endpoints Disponíveis

- **`/health`** - Status geral de saúde
- **`/health/live`** - Liveness probe (Kubernetes)
- **`/health/ready`** - Readiness probe (Kubernetes)
- **`/health/startup`** - Startup probe (Kubernetes)
- **`/health/details`** - Detalhes completos (protegido por API key)
- **`/health-dashboard`** - Dashboard web integrado

## 🚀 Integrações

### **Observabilidade**
- **OpenTelemetry**: Tracing distribuído
- **Prometheus**: Métricas e alertas
- **Serilog**: Logging estruturado
- **Grafana**: Dashboards avançados

### **Service Mesh**
- **Istio**: Configuração avançada de mesh
- **Linkerd**: Service mesh leve
- **Consul**: Service discovery e mesh

### **Cloud Native**
- **Kubernetes**: Probes automáticos
- **Docker**: Health checks de container
- **Helm**: Deployments parametrizados

## 📈 Roadmap

### **v2.1 (Próxima versão)**
- [ ] Integração com Azure Key Vault
- [ ] Suporte a AWS Secrets Manager
- [ ] Health checks para Elasticsearch
- [ ] Métricas customizáveis

### **v2.2**
- [ ] Auto-healing automático
- [ ] Integração com PagerDuty/Slack
- [ ] Health checks para Cassandra
- [ ] Suporte a múltiplos ambientes

### **v3.0 (Futuro)**
- [ ] Machine Learning avançado
- [ ] Análise de dependências entre serviços
- [ ] Health checks baseados em AI
- [ ] Integração com Service Mesh avançada

## 🧪 Testes

```bash
# Executar testes unitários
dotnet test

# Executar testes com cobertura
dotnet test --collect:"XPlat Code Coverage"

# Executar testes específicos
dotnet test --filter "FullyQualifiedName~HealthCheckBuilder"
```

## 📚 Documentação

- [**API Fluente**](examples/fluent-api-usage.md) - Como usar a nova API fluente
- [**Atributos**](attributes.md) - Uso do HealthCheckAttribute
- [**Exemplos YAML**](examples/healthchecks.yaml) - Configurações de exemplo
- [**Migração**](examples/migration-guide.md) - Guia de migração da v1 para v2

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🆘 Suporte

- **Issues**: [GitHub Issues](https://github.com/megawish/Package.HealthCheck/issues)
- **Documentação**: [docs/](docs/)
- **Exemplos**: [docs/examples/](docs/examples/)
- **Wiki**: [GitHub Wiki](https://github.com/megawish/Package.HealthCheck/wiki)

---

**⭐ Se este projeto te ajudou, considere dar uma estrela!**