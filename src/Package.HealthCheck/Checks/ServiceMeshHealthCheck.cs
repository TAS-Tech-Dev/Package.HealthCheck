using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;

namespace Package.HealthCheck.Checks;

/// <summary>
/// HealthCheck que se integra com Service Mesh (Istio, Linkerd) para monitoramento distribuído.
/// </summary>
public class ServiceMeshHealthCheck : IHealthCheck
{
    private readonly ILogger<ServiceMeshHealthCheck> _logger;
    private readonly HttpClient _httpClient;
    private readonly ServiceMeshOptions _options;

    public ServiceMeshHealthCheck(
        ILogger<ServiceMeshHealthCheck> logger,
        HttpClient httpClient,
        IOptions<ServiceMeshOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <summary>
    /// Executa o HealthCheck do Service Mesh.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executando HealthCheck do Service Mesh");

            // Verificar status no Service Mesh
            var meshStatus = await GetServiceMeshStatus(cancellationToken);
            
            // Reportar métricas para o mesh
            await ReportMetricsToMesh(meshStatus, cancellationToken);
            
            // Verificar conectividade com o mesh
            var connectivityStatus = await CheckMeshConnectivity(cancellationToken);

            if (meshStatus.IsHealthy && connectivityStatus.IsHealthy)
            {
                var data = new Dictionary<string, object>
                {
                    ["mesh_status"] = meshStatus.Status,
                    ["connectivity"] = connectivityStatus.Status,
                    ["last_check"] = DateTime.UtcNow,
                    ["mesh_type"] = _options.MeshType.ToString()
                };

                return HealthCheckResult.Healthy(
                    description: "Service Mesh está saudável",
                    data: data);
            }

            var unhealthyData = new Dictionary<string, object>
            {
                ["mesh_status"] = meshStatus.Status,
                ["connectivity"] = connectivityStatus.Status,
                ["last_check"] = DateTime.UtcNow,
                ["mesh_type"] = _options.MeshType.ToString(),
                ["errors"] = new[] { meshStatus.Error, connectivityStatus.Error }.Where(e => !string.IsNullOrEmpty(e))
            };

            return HealthCheckResult.Unhealthy(
                description: "Service Mesh não está saudável",
                data: unhealthyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante HealthCheck do Service Mesh");
            
            var errorData = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["last_check"] = DateTime.UtcNow,
                ["mesh_type"] = _options.MeshType.ToString()
            };

            return HealthCheckResult.Unhealthy(
                description: "Erro durante verificação do Service Mesh",
                exception: ex,
                data: errorData);
        }
    }

    /// <summary>
    /// Obtém o status atual do Service Mesh.
    /// </summary>
    private async Task<MeshStatus> GetServiceMeshStatus(CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = GetMeshStatusEndpoint();
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var status = JsonSerializer.Deserialize<MeshStatusResponse>(content);
                
                return new MeshStatus
                {
                    IsHealthy = status?.Status == "healthy",
                    Status = status?.Status ?? "unknown",
                    Error = status?.Error
                };
            }

            return new MeshStatus
            {
                IsHealthy = false,
                Status = "unhealthy",
                Error = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter status do Service Mesh");
            return new MeshStatus
            {
                IsHealthy = false,
                Status = "error",
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Reporta métricas para o Service Mesh.
    /// </summary>
    private async Task ReportMetricsToMesh(MeshStatus status, CancellationToken cancellationToken)
    {
        try
        {
            var metrics = new
            {
                timestamp = DateTime.UtcNow,
                service_name = _options.ServiceName,
                mesh_type = _options.MeshType.ToString(),
                status = status.Status,
                is_healthy = status.IsHealthy
            };

            var endpoint = GetMetricsEndpoint();
            var content = new StringContent(JsonSerializer.Serialize(metrics));
            
            await _httpClient.PostAsync(endpoint, content, cancellationToken);
            
            _logger.LogDebug("Métricas reportadas para o Service Mesh");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao reportar métricas para o Service Mesh");
        }
    }

    /// <summary>
    /// Verifica a conectividade com o Service Mesh.
    /// </summary>
    private async Task<ConnectivityStatus> CheckMeshConnectivity(CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = GetConnectivityEndpoint();
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            
            return new ConnectivityStatus
            {
                IsHealthy = response.IsSuccessStatusCode,
                Status = response.IsSuccessStatusCode ? "connected" : "disconnected",
                Error = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ConnectivityStatus
            {
                IsHealthy = false,
                Status = "error",
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Obtém o endpoint para verificar status do mesh.
    /// </summary>
    private string GetMeshStatusEndpoint()
    {
        return _options.MeshType switch
        {
            MeshType.Istio => $"{_options.BaseUrl}/healthz/ready",
            MeshType.Linkerd => $"{_options.BaseUrl}/ping",
            _ => $"{_options.BaseUrl}/health"
        };
    }

    /// <summary>
    /// Obtém o endpoint para reportar métricas.
    /// </summary>
    private string GetMetricsEndpoint()
    {
        return _options.MeshType switch
        {
            MeshType.Istio => $"{_options.BaseUrl}/metrics",
            MeshType.Linkerd => $"{_options.BaseUrl}/metrics",
            _ => $"{_options.BaseUrl}/metrics"
        };
    }

    /// <summary>
    /// Obtém o endpoint para verificar conectividade.
    /// </summary>
    private string GetConnectivityEndpoint()
    {
        return _options.MeshType switch
        {
            MeshType.Istio => $"{_options.BaseUrl}/healthz",
            MeshType.Linkerd => $"{_options.BaseUrl}/ping",
            _ => $"{_options.BaseUrl}/ping"
        };
    }
}

/// <summary>
/// Opções de configuração para o Service Mesh.
/// </summary>
public class ServiceMeshOptions
{
    /// <summary>
    /// URL base do Service Mesh.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:15020";

    /// <summary>
    /// Tipo de Service Mesh.
    /// </summary>
    public MeshType MeshType { get; set; } = MeshType.Istio;

    /// <summary>
    /// Nome do serviço atual.
    /// </summary>
    public string ServiceName { get; set; } = "unknown";

    /// <summary>
    /// Timeout para requisições HTTP em segundos.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Indica se deve reportar métricas para o mesh.
    /// </summary>
    public bool ReportMetrics { get; set; } = true;
}

/// <summary>
/// Tipos de Service Mesh suportados.
/// </summary>
public enum MeshType
{
    /// <summary>
    /// Istio Service Mesh.
    /// </summary>
    Istio,

    /// <summary>
    /// Linkerd Service Mesh.
    /// </summary>
    Linkerd,

    /// <summary>
    /// Consul Connect.
    /// </summary>
    Consul,

    /// <summary>
    /// Outro tipo de Service Mesh.
    /// </summary>
    Other
}

/// <summary>
/// Status do Service Mesh.
/// </summary>
public class MeshStatus
{
    /// <summary>
    /// Indica se o mesh está saudável.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Status atual do mesh.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Erro, se houver.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Status de conectividade.
/// </summary>
public class ConnectivityStatus
{
    /// <summary>
    /// Indica se há conectividade.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Status da conectividade.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Erro, se houver.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Resposta do status do mesh.
/// </summary>
public class MeshStatusResponse
{
    /// <summary>
    /// Status do mesh.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Erro, se houver.
    /// </summary>
    public string? Error { get; set; }
}
