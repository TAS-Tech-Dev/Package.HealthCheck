# HealthChecks Baseados em Atributos

Este documento demonstra como usar os atributos `HealthCheckAttribute` para configurar HealthChecks de forma declarativa e auto-documentada.

## Visão Geral

Os atributos permitem marcar classes que precisam de HealthChecks específicos, facilitando a configuração e manutenção do código.

## Atributos Disponíveis

### HealthCheckAttribute

```csharp
[HealthCheck(
    name: "user-database",
    type: HealthCheckType.Database,
    tags: new[] { "database", "critical", "user" },
    timeoutSeconds: 30,
    isCritical: true)]
public class UserRepository
{
    // Implementação do repositório
}
```

### Parâmetros do Atributo

- **name**: Nome único do HealthCheck
- **type**: Tipo do HealthCheck (Database, Http, MessageQueue, etc.)
- **tags**: Array de tags para categorização
- **timeoutSeconds**: Timeout em segundos para o HealthCheck
- **isCritical**: Se é crítico para a aplicação
- **configuration**: Configurações específicas em JSON (opcional)

## Exemplos de Uso

### 1. HealthCheck para Banco de Dados

```csharp
[HealthCheck("user-database", HealthCheckType.Database, new[] { "database", "critical", "user" }, 30, true)]
public class UserRepository : IUserRepository
{
    private readonly DbContext _context;
    
    public UserRepository(DbContext context)
    {
        _context = context;
    }
    
    // Implementação dos métodos...
}
```

### 2. HealthCheck para Serviço HTTP

```csharp
[HealthCheck("payment-api", HealthCheckType.Http, new[] { "external", "critical", "payment" }, 10, true)]
public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    
    public PaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    // Implementação dos métodos...
}
```

### 3. HealthCheck para Cache

```csharp
[HealthCheck("user-cache", HealthCheckType.Cache, new[] { "cache", "performance", "user" }, 5, false)]
public class UserCacheService : IUserCacheService
{
    private readonly IDistributedCache _cache;
    
    public UserCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }
    
    // Implementação dos métodos...
}
```

### 4. HealthCheck para Fila de Mensagens

```csharp
[HealthCheck("notification-queue", HealthCheckType.MessageQueue, new[] { "queue", "async", "notification" }, 15, false)]
public class NotificationQueueService : INotificationQueueService
{
    private readonly IConnection _connection;
    
    public NotificationQueueService(IConnection connection)
    {
        _connection = connection;
    }
    
    // Implementação dos métodos...
}
```

### 5. HealthCheck para Sistema de Arquivos

```csharp
[HealthCheck("file-storage", HealthCheckType.FileSystem, new[] { "storage", "files", "critical" }, 20, true)]
public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    
    public FileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["FileStorage:BasePath"];
    }
    
    // Implementação dos métodos...
}
```

### 6. HealthCheck para Recursos de Sistema

```csharp
[HealthCheck("memory-monitor", HealthCheckType.Memory, new[] { "system", "resources", "monitoring" }, 10, false)]
public class MemoryMonitorService : IMemoryMonitorService
{
    public MemoryInfo GetMemoryInfo()
    {
        var process = Process.GetCurrentProcess();
        return new MemoryInfo
        {
            WorkingSet = process.WorkingSet64,
            PeakWorkingSet = process.PeakWorkingSet64,
            VirtualMemorySize = process.VirtualMemorySize64
        };
    }
}
```

## Configuração Avançada

### Configuração em JSON

```csharp
[HealthCheck(
    "custom-check",
    HealthCheckType.Custom,
    new[] { "custom", "business" },
    60,
    false,
    configuration: @"{""customParam"": ""value"", ""threshold"": 100}")]
public class CustomBusinessService
{
    // Implementação...
}
```

### Múltiplos Atributos (Não Permitido)

```csharp
// ❌ ERRO: Não é permitido múltiplos atributos HealthCheck na mesma classe
[HealthCheck("check1", HealthCheckType.Database)]
[HealthCheck("check2", HealthCheckType.Http)] // Isso causará erro de compilação
public class InvalidService
{
    // Implementação...
}
```

## Integração com Auto-Discovery

Quando o auto-discovery está habilitado, o sistema automaticamente detecta classes com atributos `HealthCheckAttribute` e cria HealthChecks apropriados.

### Configuração no appsettings.json

```json
{
  "HealthCheck": {
    "EnableAutoDiscovery": true
  }
}
```

### Configuração via Código

```csharp
services.AddMegaWishHealthChecks(configuration, options =>
{
    options.EnableAutoDiscovery = true;
});
```

## Tags Recomendadas

### Tags de Categoria
- `database`: Para HealthChecks de banco de dados
- `external`: Para dependências externas
- `internal`: Para serviços internos
- `infra`: Para infraestrutura
- `business`: Para lógica de negócio

### Tags de Criticidade
- `critical`: Para componentes críticos
- `important`: Para componentes importantes
- `noncritical`: Para componentes não críticos

### Tags de Tipo
- `readiness`: Para HealthChecks de readiness
- `liveness`: Para HealthChecks de liveness
- `startup`: Para HealthChecks de startup

### Tags de Ambiente
- `production`: Para produção
- `staging`: Para staging
- `development`: Para desenvolvimento

## Exemplo Completo

```csharp
using Package.HealthCheck.Attributes;

namespace MyApp.Services
{
    [HealthCheck("user-database", HealthCheckType.Database, new[] { "database", "critical", "user", "readiness" }, 30, true)]
    public class UserRepository : IUserRepository
    {
        private readonly DbContext _context;
        private readonly ILogger<UserRepository> _logger;
        
        public UserRepository(DbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<User?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Users.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário {UserId}", id);
                throw;
            }
        }
        
        // Outros métodos...
    }
    
    [HealthCheck("payment-api", HealthCheckType.Http, new[] { "external", "critical", "payment", "readiness" }, 10, true)]
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentService> _logger;
        
        public PaymentService(HttpClient httpClient, ILogger<PaymentService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        
        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/payments", request);
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<PaymentResult>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar pagamento");
                throw;
            }
        }
    }
}
```

## Benefícios

1. **Configuração Declarativa**: HealthChecks são configurados diretamente nas classes
2. **Auto-Documentação**: O código documenta automaticamente suas dependências de saúde
3. **Manutenção Simplificada**: Mudanças na configuração são feitas junto com o código
4. **Consistência**: Padrões consistentes de configuração em toda a aplicação
5. **Integração com Auto-Discovery**: Funciona perfeitamente com o sistema de descoberta automática

## Considerações

- Use tags descritivas e consistentes
- Configure timeouts apropriados para cada tipo de HealthCheck
- Marque apenas componentes que realmente precisam de monitoramento
- Use a configuração JSON para parâmetros complexos
- Mantenha as tags organizadas por categoria
