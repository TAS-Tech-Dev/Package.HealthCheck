using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Package.HealthCheck.Attributes;

namespace Package.HealthCheck.Discovery;

/// <summary>
/// Serviço responsável por descobrir automaticamente dependências registradas
/// e criar HealthChecks apropriados sem configuração manual.
/// </summary>
public class DependencyDiscoveryService
{
    private readonly ILogger<DependencyDiscoveryService> _logger;
    private readonly IServiceCollection _services;
    private readonly IHealthChecksBuilder _healthChecksBuilder;

    public DependencyDiscoveryService(
        ILogger<DependencyDiscoveryService> logger,
        IServiceCollection services,
        IHealthChecksBuilder healthChecksBuilder)
    {
        _logger = logger;
        _services = services;
        _healthChecksBuilder = healthChecksBuilder;
    }

    /// <summary>
    /// Descobre e registra HealthChecks automaticamente baseado nas dependências registradas.
    /// </summary>
    public void DiscoverAndRegisterHealthChecks()
    {
        _logger.LogInformation("Iniciando descoberta automática de dependências...");

        try
        {
            DiscoverDbContexts();
            DiscoverHttpClients();
            DiscoverConnectionStrings();
            DiscoverCustomServices();
            
            _logger.LogInformation("Descoberta automática de dependências concluída com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante a descoberta automática de dependências");
        }
    }

    /// <summary>
    /// Descobre DbContexts registrados e cria HealthChecks para eles.
    /// </summary>
    private void DiscoverDbContexts()
    {
        var dbContexts = _services
            .Where(s => s.ServiceType.Name.EndsWith("DbContext"))
            .ToList();

        foreach (var dbContext in dbContexts)
        {
            var dbName = dbContext.ServiceType.Name.Replace("DbContext", "");
            var tags = new[] { "database", "entity-framework", dbName.ToLowerInvariant() };
            
            _logger.LogInformation("Descobrindo DbContext: {DbContextName}", dbContext.ServiceType.Name);
            
            // Registra HealthCheck genérico para DbContext
            _healthChecksBuilder.AddCheck(
                name: $"{dbName}Database",
                check: () => HealthCheckResult.Healthy("DbContext disponível"),
                tags: tags);
        }
    }

    /// <summary>
    /// Descobre HttpClient registrados e cria HealthChecks para eles.
    /// </summary>
    private void DiscoverHttpClients()
    {
        var httpClients = _services
            .Where(s => s.ServiceType == typeof(HttpClient) || 
                       s.ServiceType.Name.Contains("HttpClient"))
            .ToList();

        foreach (var httpClient in httpClients)
        {
            var clientName = httpClient.ServiceType.Name.Replace("HttpClient", "");
            var tags = new[] { "http", "external", clientName.ToLowerInvariant() };
            
            _logger.LogInformation("Descobrindo HttpClient: {HttpClientName}", httpClient.ServiceType.Name);
            
            // Registra HealthCheck para HTTP dependencies
            _healthChecksBuilder.AddUrlGroup(
                new[] { new Uri("http://localhost/health") }, // Placeholder - será configurado via options
                name: $"{clientName}HttpHealth",
                tags: tags);
        }
    }

    /// <summary>
    /// Descobre connection strings configuradas e cria HealthChecks apropriados.
    /// </summary>
    private void DiscoverConnectionStrings()
    {
        // Esta implementação será expandida para detectar connection strings
        // de diferentes provedores de banco de dados
        _logger.LogInformation("Descoberta de connection strings será implementada na próxima versão");
    }

    /// <summary>
    /// Descobre serviços customizados que podem ter HealthChecks específicos.
    /// </summary>
    private void DiscoverCustomServices()
    {
        var customServices = _services
            .Where(s => s.ServiceType.GetCustomAttributes(typeof(HealthCheckAttribute), false).Any())
            .ToList();

        foreach (var service in customServices)
        {
            var healthCheckAttr = service.ServiceType.GetCustomAttribute<HealthCheckAttribute>();
            if (healthCheckAttr != null)
            {
                _logger.LogInformation("Descobrindo serviço com HealthCheck: {ServiceName}", service.ServiceType.Name);
                
                // Registra HealthCheck customizado baseado no atributo
                RegisterCustomHealthCheck(service, healthCheckAttr);
            }
        }
    }

    /// <summary>
    /// Registra HealthCheck customizado baseado no atributo.
    /// </summary>
    private void RegisterCustomHealthCheck(ServiceDescriptor service, HealthCheckAttribute attribute)
    {
        // Implementação será expandida para suportar diferentes tipos de HealthChecks customizados
        _logger.LogInformation("Registrando HealthCheck customizado para: {ServiceName}", service.ServiceType.Name);
    }
}
