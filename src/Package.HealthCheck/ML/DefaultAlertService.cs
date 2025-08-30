using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Package.HealthCheck.ML;

/// <summary>
/// Implementação padrão do serviço de alertas.
/// Registra alertas em log e pode ser estendida para outros canais.
/// </summary>
public class DefaultAlertService : IAlertService
{
    private readonly ILogger<DefaultAlertService> _logger;
    private readonly AlertServiceOptions _options;
    private readonly List<HealthAlert> _recentAlerts;
    private readonly object _lock = new object();

    public DefaultAlertService(
        ILogger<DefaultAlertService> logger,
        IOptions<AlertServiceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _recentAlerts = new List<HealthAlert>();
    }

    /// <summary>
    /// Envia um alerta.
    /// </summary>
    public async Task SendAlertAsync(HealthAlert alert, CancellationToken cancellationToken = default)
    {
        if (alert == null)
            throw new ArgumentNullException(nameof(alert));

        try
        {
            // Registrar alerta no histórico local
            lock (_lock)
            {
                _recentAlerts.Add(alert);
                
                // Manter apenas os últimos 100 alertas
                if (_recentAlerts.Count > 100)
                {
                    _recentAlerts.RemoveRange(0, _recentAlerts.Count - 100);
                }
            }

            // Log do alerta baseado na severidade
            LogAlert(alert);

            // Verificar se deve enviar para canais externos
            if (_options.EnableExternalChannels)
            {
                await SendToExternalChannels(alert, cancellationToken);
            }

            // Verificar se deve executar ações automáticas
            if (_options.EnableAutoActions && alert.Severity == AlertSeverity.High)
            {
                await ExecuteAutoActions(alert, cancellationToken);
            }

            _logger.LogDebug("Alerta enviado com sucesso: {AlertType} para {Component}", 
                alert.Type, alert.Component);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar alerta: {AlertType} para {Component}", 
                alert.Type, alert.Component);
            throw;
        }
    }

    /// <summary>
    /// Obtém alertas recentes.
    /// </summary>
    public Task<IEnumerable<HealthAlert>> GetRecentAlertsAsync(
        TimeSpan period,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(period);
        
        lock (_lock)
        {
            var result = _recentAlerts
                .Where(a => a.Timestamp >= cutoffTime)
                .OrderByDescending(a => a.Timestamp)
                .ToList();

            return Task.FromResult<IEnumerable<HealthAlert>>(result);
        }
    }

    /// <summary>
    /// Obtém estatísticas dos alertas.
    /// </summary>
    public Task<AlertStats> GetAlertStatsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var last24h = now.Subtract(TimeSpan.FromHours(24));
            var last7d = now.Subtract(TimeSpan.FromDays(7));

            var stats = new AlertStats
            {
                TotalAlerts = _recentAlerts.Count,
                AlertsLast24Hours = _recentAlerts.Count(a => a.Timestamp >= last24h),
                AlertsLast7Days = _recentAlerts.Count(a => a.Timestamp >= last7d),
                CriticalAlerts = _recentAlerts.Count(a => a.Type == AlertType.Critical),
                HighSeverityAlerts = _recentAlerts.Count(a => a.Severity == AlertSeverity.High),
                ComponentsWithAlerts = _recentAlerts.Select(a => a.Component).Distinct().Count(),
                LastAlertTime = _recentAlerts.Any() ? _recentAlerts.Max(a => a.Timestamp) : DateTime.UtcNow
            };

            return Task.FromResult(stats);
        }
    }

    /// <summary>
    /// Registra o alerta no log baseado na severidade.
    /// </summary>
    private void LogAlert(HealthAlert alert)
    {
        var logLevel = alert.Severity switch
        {
            AlertSeverity.Low => LogLevel.Information,
            AlertSeverity.Medium => LogLevel.Warning,
            AlertSeverity.High => LogLevel.Error,
            _ => LogLevel.Information
        };

        var logMessage = $"ALERTA [{alert.Type}] {alert.Severity}: {alert.Message} | Componente: {alert.Component} | Timestamp: {alert.Timestamp:yyyy-MM-dd HH:mm:ss}";

        if (alert.Data?.Any() == true)
        {
            logMessage += $" | Dados: {string.Join(", ", alert.Data.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
        }

        _logger.Log(logLevel, logMessage);
    }

    /// <summary>
    /// Envia alerta para canais externos.
    /// </summary>
    private async Task SendToExternalChannels(HealthAlert alert, CancellationToken cancellationToken)
    {
        try
        {
            // Aqui você pode implementar integrações com:
            // - Slack
            // - Teams
            // - Email
            // - SMS
            // - Webhooks
            // - etc.

            if (_options.EnableSlackIntegration)
            {
                await SendToSlack(alert, cancellationToken);
            }

            if (_options.EnableEmailIntegration)
            {
                await SendToEmail(alert, cancellationToken);
            }

            if (_options.EnableWebhookIntegration)
            {
                await SendToWebhook(alert, cancellationToken);
            }

            _logger.LogDebug("Alerta enviado para canais externos: {Component}", alert.Component);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao enviar alerta para canais externos: {Component}", alert.Component);
        }
    }

    /// <summary>
    /// Executa ações automáticas para alertas críticos.
    /// </summary>
    private async Task ExecuteAutoActions(HealthAlert alert, CancellationToken cancellationToken)
    {
        try
        {
            // Aqui você pode implementar ações automáticas como:
            // - Restart de serviços
            // - Escalar recursos
            // - Notificar equipes específicas
            // - Executar scripts de recuperação

            if (alert.Type == AlertType.Critical)
            {
                _logger.LogInformation("Executando ações automáticas para alerta crítico: {Component}", alert.Component);
                
                // Exemplo: agendar restart automático
                if (_options.EnableAutoRestart)
                {
                    await ScheduleAutoRestart(alert, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao executar ações automáticas para: {Component}", alert.Component);
        }
    }

    /// <summary>
    /// Envia alerta para Slack (placeholder).
    /// </summary>
    private Task SendToSlack(HealthAlert alert, CancellationToken cancellationToken)
    {
        // Implementar integração com Slack
        return Task.CompletedTask;
    }

    /// <summary>
    /// Envia alerta por email (placeholder).
    /// </summary>
    private Task SendToEmail(HealthAlert alert, CancellationToken cancellationToken)
    {
        // Implementar integração com email
        return Task.CompletedTask;
    }

    /// <summary>
    /// Envia alerta para webhook (placeholder).
    /// </summary>
    private Task SendToWebhook(HealthAlert alert, CancellationToken cancellationToken)
    {
        // Implementar integração com webhook
        return Task.CompletedTask;
    }

    /// <summary>
    /// Agenda restart automático (placeholder).
    /// </summary>
    private Task ScheduleAutoRestart(HealthAlert alert, CancellationToken cancellationToken)
    {
        // Implementar lógica de restart automático
        return Task.CompletedTask;
    }
}

/// <summary>
/// Opções de configuração para o serviço de alertas.
/// </summary>
public class AlertServiceOptions
{
    /// <summary>
    /// Habilita canais externos de alerta.
    /// </summary>
    public bool EnableExternalChannels { get; set; } = false;

    /// <summary>
    /// Habilita ações automáticas.
    /// </summary>
    public bool EnableAutoActions { get; set; } = false;

    /// <summary>
    /// Habilita integração com Slack.
    /// </summary>
    public bool EnableSlackIntegration { get; set; } = false;

    /// <summary>
    /// Habilita integração com email.
    /// </summary>
    public bool EnableEmailIntegration { get; set; } = false;

    /// <summary>
    /// Habilita integração com webhook.
    /// </summary>
    public bool EnableWebhookIntegration { get; set; } = false;

    /// <summary>
    /// Habilita restart automático.
    /// </summary>
    public bool EnableAutoRestart { get; set; } = false;

    /// <summary>
    /// URL do webhook para alertas.
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Token do Slack.
    /// </summary>
    public string? SlackToken { get; set; }

    /// <summary>
    /// Canal do Slack.
    /// </summary>
    public string? SlackChannel { get; set; }
}

/// <summary>
/// Estatísticas dos alertas.
/// </summary>
public class AlertStats
{
    /// <summary>
    /// Total de alertas.
    /// </summary>
    public int TotalAlerts { get; set; }

    /// <summary>
    /// Alertas nas últimas 24 horas.
    /// </summary>
    public int AlertsLast24Hours { get; set; }

    /// <summary>
    /// Alertas nos últimos 7 dias.
    /// </summary>
    public int AlertsLast7Days { get; set; }

    /// <summary>
    /// Alertas críticos.
    /// </summary>
    public int CriticalAlerts { get; set; }

    /// <summary>
    /// Alertas de alta severidade.
    /// </summary>
    public int HighSeverityAlerts { get; set; }

    /// <summary>
    /// Número de componentes com alertas.
    /// </summary>
    public int ComponentsWithAlerts { get; set; }

    /// <summary>
    /// Horário do último alerta.
    /// </summary>
    public DateTime LastAlertTime { get; set; }
}
