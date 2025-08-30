using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Package.HealthCheck.Core;
using System.Text.Json;

namespace Package.HealthCheck.Dashboard;

/// <summary>
/// Controller para o dashboard web de HealthChecks.
/// Fornece interface para visualizar status em tempo real.
/// </summary>
[ApiController]
[Route("health-dashboard")]
public class HealthDashboardController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IOptions<HealthConfig> _healthConfig;
    private readonly ILogger<HealthDashboardController> _logger;

    public HealthDashboardController(
        HealthCheckService healthCheckService,
        IOptions<HealthConfig> healthConfig,
        ILogger<HealthDashboardController> logger)
    {
        _healthCheckService = healthCheckService;
        _healthConfig = healthConfig;
        _logger = logger;
    }

    /// <summary>
    /// Página principal do dashboard.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return Content(GetDashboardHtml(), "text/html");
    }

    /// <summary>
    /// API para obter status dos HealthChecks em formato JSON.
    /// </summary>
    [HttpGet("api/status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();
            
            var dashboardData = new
            {
                timestamp = DateTime.UtcNow,
                status = report.Status.ToString(),
                total_checks = report.Entries.Count,
                healthy_checks = report.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                degraded_checks = report.Entries.Count(e => e.Value.Status == HealthStatus.Degraded),
                unhealthy_checks = report.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy),
                entries = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                    tags = e.Value.Tags,
                    data = e.Value.Data
                })
            };

            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter status dos HealthChecks");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// API para obter status de um HealthCheck específico.
    /// </summary>
    [HttpGet("api/status/{checkName}")]
    public async Task<IActionResult> GetCheckStatus(string checkName)
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();
            
            if (!report.Entries.TryGetValue(checkName, out var entry))
            {
                return NotFound(new { error = $"HealthCheck '{checkName}' não encontrado" });
            }

            var checkData = new
            {
                name = checkName,
                status = entry.Status.ToString(),
                description = entry.Description,
                duration = entry.Duration.TotalMilliseconds,
                tags = entry.Tags,
                data = entry.Data,
                timestamp = DateTime.UtcNow
            };

            return Ok(checkData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter status do HealthCheck: {CheckName}", checkName);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// API para obter métricas do dashboard.
    /// </summary>
    [HttpGet("api/metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();
            
            var metrics = new
            {
                health_status = report.Status == HealthStatus.Healthy ? 1 : 0,
                health_last_change_timestamp_seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                health_checks_total = report.Entries.Count,
                health_checks_healthy = report.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                health_checks_degraded = report.Entries.Count(e => e.Value.Status == HealthStatus.Degraded),
                health_checks_unhealthy = report.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy)
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter métricas do dashboard");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gera o HTML do dashboard.
    /// </summary>
    private string GetDashboardHtml()
    {
        return @"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Health Dashboard - Package.HealthCheck</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; padding: 20px; }
        .header { background: #fff; padding: 20px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header h1 { color: #333; margin-bottom: 10px; }
        .header .status { font-size: 18px; font-weight: 500; }
        .status.healthy { color: #28a745; }
        .status.degraded { color: #ffc107; }
        .status.unhealthy { color: #dc3545; }
        .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 20px; }
        .stat-card { background: #fff; padding: 20px; border-radius: 8px; text-align: center; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .stat-number { font-size: 32px; font-weight: bold; margin-bottom: 5px; }
        .stat-label { color: #666; font-size: 14px; }
        .checks-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }
        .check-card { background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .check-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px; }
        .check-name { font-weight: bold; color: #333; }
        .check-status { padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: 500; }
        .check-status.healthy { background: #d4edda; color: #155724; }
        .check-status.degraded { background: #fff3cd; color: #856404; }
        .check-status.unhealthy { background: #f8d7da; color: #721c24; }
        .check-description { color: #666; margin-bottom: 10px; }
        .check-details { font-size: 12px; color: #999; }
        .refresh-btn { background: #007bff; color: #fff; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer; margin-bottom: 20px; }
        .refresh-btn:hover { background: #0056b3; }
        .auto-refresh { margin-left: 10px; }
        .timestamp { text-align: center; color: #666; font-size: 12px; margin-top: 20px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏥 Health Dashboard</h1>
            <div class='status' id='overall-status'>Carregando...</div>
        </div>
        
        <button class='refresh-btn' onclick='refreshData()'>🔄 Atualizar</button>
        <label class='auto-refresh'>
            <input type='checkbox' id='auto-refresh' onchange='toggleAutoRefresh()'> Auto-refresh (30s)
        </label>
        
        <div class='stats' id='stats'>
            <div class='stat-card'>
                <div class='stat-number' id='total-checks'>-</div>
                <div class='stat-label'>Total de Checks</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number' id='healthy-checks'>-</div>
                <div class='stat-label'>Saudáveis</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number' id='degraded-checks'>-</div>
                <div class='stat-label'>Degradados</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number' id='unhealthy-checks'>-</div>
                <div class='stat-label'>Não Saudáveis</div>
            </div>
        </div>
        
        <div class='checks-grid' id='checks-grid'>
            <div class='check-card'>
                <div class='check-description'>Carregando HealthChecks...</div>
            </div>
        </div>
        
        <div class='timestamp' id='timestamp'>Última atualização: -</div>
    </div>

    <script>
        let autoRefreshInterval;
        
        function refreshData() {
            fetch('/health-dashboard/api/status')
                .then(response => response.json())
                .then(data => updateDashboard(data))
                .catch(error => console.error('Erro ao atualizar dashboard:', error));
        }
        
        function updateDashboard(data) {
            // Atualizar status geral
            const overallStatus = document.getElementById('overall-status');
            overallStatus.textContent = `Status Geral: ${data.status}`;
            overallStatus.className = `status ${data.status.toLowerCase()}`;
            
            // Atualizar estatísticas
            document.getElementById('total-checks').textContent = data.total_checks;
            document.getElementById('healthy-checks').textContent = data.healthy_checks;
            document.getElementById('degraded-checks').textContent = data.degraded_checks;
            document.getElementById('unhealthy-checks').textContent = data.unhealthy_checks;
            
            // Atualizar grid de checks
            const checksGrid = document.getElementById('checks-grid');
            checksGrid.innerHTML = '';
            
            Object.values(data.entries).forEach(entry => {
                const checkCard = document.createElement('div');
                checkCard.className = 'check-card';
                
                const statusClass = entry.status.toLowerCase();
                const statusText = entry.status === 'Healthy' ? 'Saudável' : 
                                 entry.status === 'Degraded' ? 'Degradado' : 'Não Saudável';
                
                checkCard.innerHTML = `
                    <div class='check-header'>
                        <div class='check-name'>${entry.name}</div>
                        <div class='check-status ${statusClass}'>${statusText}</div>
                    </div>
                    <div class='check-description'>${entry.description || 'Sem descrição'}</div>
                    <div class='check-details'>
                        Duração: ${entry.duration.toFixed(2)}ms<br>
                        Tags: ${entry.tags ? entry.tags.join(', ') : 'Nenhuma'}
                    </div>
                `;
                
                checksGrid.appendChild(checkCard);
            });
            
            // Atualizar timestamp
            document.getElementById('timestamp').textContent = `Última atualização: ${new Date().toLocaleString('pt-BR')}`;
        }
        
        function toggleAutoRefresh() {
            const checkbox = document.getElementById('auto-refresh');
            if (checkbox.checked) {
                autoRefreshInterval = setInterval(refreshData, 30000);
            } else {
                if (autoRefreshInterval) {
                    clearInterval(autoRefreshInterval);
                }
            }
        }
        
        // Carregar dados iniciais
        refreshData();
        
        // Configurar auto-refresh se marcado
        document.addEventListener('DOMContentLoaded', function() {
            const checkbox = document.getElementById('auto-refresh');
            if (checkbox.checked) {
                toggleAutoRefresh();
            }
        });
    </script>
</body>
</html>";
    }
}
