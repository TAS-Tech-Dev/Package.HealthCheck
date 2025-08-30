# Arquitetura e Design

## Visão Geral da Arquitetura

O Package.HealthCheck foi projetado seguindo princípios de arquitetura limpa, com separação clara de responsabilidades e alta coesão entre componentes relacionados.

## 🏗️ Estrutura de Camadas

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   Endpoints     │  │   Integration   │  │   Checks    │ │
│  │                 │  │                 │  │             │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                     Core Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   Options       │  │   Extensions    │  │   Models    │ │
│  │                 │  │                 │  │             │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   HealthChecks  │  │   OpenTelemetry │  │  Prometheus │ │
│  │   Framework     │  │                 │  │             │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## 🔧 Componentes Principais

### 1. Core Layer

#### HealthOptions.cs
- **Responsabilidade**: Definição de modelos de configuração
- **Padrão**: POCOs (Plain Old CLR Objects) para configuração
- **Estrutura**:
  - `HealthConfig`: Configuração principal do sistema
  - `DependenciesConfig`: Configuração de dependências
  - `MegaWishHealthOptions`: Opções de configuração via código

#### MegaWishHealthOptions.Registration.cs
- **Responsabilidade**: Fluent API para registro de health checks
- **Padrão**: Builder Pattern com method chaining
- **Funcionalidades**:
  - Registro de dependências de infraestrutura
  - Configuração de health checks customizados
  - Sistema de tags automático

### 2. Application Layer

#### ServiceCollectionExtensions.cs
- **Responsabilidade**: Configuração do sistema de DI
- **Padrão**: Extension Method para IServiceCollection
- **Funcionalidades**:
  - Registro automático de health checks baseado na configuração
  - Configuração de OpenTelemetry
  - Registro do background worker

#### HealthEndpointMappings.cs
- **Responsabilidade**: Mapeamento de endpoints HTTP
- **Padrão**: Extension Method para IApplicationBuilder
- **Endpoints**:
  - `/health/live`: Probe de liveness
  - `/health/ready`: Probe de readiness
  - `/health/startup`: Probe de startup
  - `/health/details`: Detalhes completos

### 3. Checks Layer

#### StartupGate.cs
- **Responsabilidade**: Controle de estado de inicialização
- **Padrão**: Signal Pattern
- **Funcionalidades**:
  - `StartupSignal`: Sinalizador de estado
  - `StartupGateHealthCheck`: Health check baseado no sinal

#### DiskSpaceHealthCheck.cs
- **Responsabilidade**: Monitoramento de espaço em disco
- **Padrão**: Strategy Pattern (implementa IHealthCheck)
- **Funcionalidades**:
  - Verificação de espaço livre
  - Configuração de limite mínimo
  - Suporte a múltiplos drives

#### WorkingSetHealthCheck.cs
- **Responsabilidade**: Monitoramento de memória do processo
- **Padrão**: Strategy Pattern
- **Funcionalidades**:
  - Verificação de Working Set
  - Configuração de limite máximo

#### HttpDependencyHealthCheck.cs
- **Responsabilidade**: Verificação de dependências HTTP
- **Padrão**: Strategy Pattern
- **Funcionalidades**:
  - Timeout configurável
  - Verificação de status HTTP
  - Tratamento de exceções

### 4. Integration Layer

#### HealthBackgroundWorker.cs
- **Responsabilidade**: Monitoramento contínuo e publicação
- **Padrão**: Background Service
- **Funcionalidades**:
  - Coleta de métricas Prometheus
  - Publicação para RabbitMQ
  - Logging de mudanças de estado

## 🔄 Fluxo de Dados

### 1. Inicialização
```
Program.cs → AddMegaWishHealthChecks() → ServiceCollectionExtensions
    ↓
Configuração lida do appsettings.json
    ↓
Health checks registrados no container DI
    ↓
Background worker iniciado
```

### 2. Execução de Health Checks
```
HTTP Request → Endpoint → HealthCheckService
    ↓
Execução de todos os health checks registrados
    ↓
Agregação de resultados
    ↓
Resposta HTTP com status apropriado
```

### 3. Monitoramento Contínuo
```
Background Worker → HealthCheckService → Health Checks
    ↓
Coleta de métricas
    ↓
Detecção de mudanças de estado
    ↓
Publicação para sistemas externos
```

## 🎯 Padrões de Design Utilizados

### 1. Builder Pattern
- **Uso**: `MegaWishHealthOptions` para configuração fluente
- **Benefícios**: Configuração legível e encadeável

### 2. Strategy Pattern
- **Uso**: Todos os health checks implementam `IHealthCheck`
- **Benefícios**: Facilita extensibilidade e testes

### 3. Extension Method Pattern
- **Uso**: `ServiceCollectionExtensions` e `HealthEndpointMappings`
- **Benefícios**: API limpa e intuitiva

### 4. Signal Pattern
- **Uso**: `StartupSignal` para controle de estado
- **Benefícios**: Controle simples e eficiente de inicialização

### 5. Factory Pattern
- **Uso**: Criação de health checks via `HealthCheckRegistration`
- **Benefícios**: Instanciação lazy e controlada

## 🔒 Segurança e Configuração

### 1. Autenticação
- **Endpoint**: `/health/details`
- **Método**: API Key via header `X-Health-ApiKey`
- **Configuração**: Via `HealthDetailsAuthOptions`

### 2. Configuração
- **Fonte**: `appsettings.json` + configuração via código
- **Validação**: Binding automático via `IConfiguration.Bind()`
- **Override**: Configuração via código tem precedência

## 📊 Observabilidade

### 1. Métricas (Prometheus)
- **Gauge**: Status de cada health check
- **Labels**: `service`, `check`
- **Valores**: 1 (Healthy), 0 (Degraded), -1 (Unhealthy)

### 2. Logs (Serilog)
- **Estrutura**: Logging estruturado
- **Eventos**: Mudanças de estado de saúde
- **Contexto**: Nome do serviço e status

### 3. Tracing (OpenTelemetry)
- **Source**: `Package.HealthCheck`
- **Escopo**: Avaliação de health checks
- **Integração**: Configuração automática

## 🚀 Pontos de Extensibilidade

### 1. Health Checks Customizados
```csharp
public class CustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Implementação customizada
    }
}
```

### 2. Configuração via Fluent API
```csharp
options.UseCustomHealthCheck("custom", sp => new CustomHealthCheck());
```

### 3. Tags e Metadados
```csharp
options.UsePostgres("db", connectionString, critical: true, tags: new[] { "database", "primary" });
```

## 🔮 Arquitetura Futura (Discovery Automático)

### 1. Reflection-based Discovery
```
Assembly Scanning → Type Discovery → Health Check Registration
    ↓
Attribute-based Configuration
    ↓
Automatic Health Check Creation
```

### 2. Configuration-driven Health Checks
```json
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

### 3. Service Mesh Integration
```
Service Discovery → Health Check Registration
    ↓
Dynamic Configuration
    ↓
Automatic Endpoint Creation
```

Esta arquitetura fornece uma base sólida para extensões futuras, mantendo a simplicidade de uso atual enquanto permite crescimento e complexidade controlada.
