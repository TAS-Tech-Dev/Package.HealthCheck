using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.HealthCheck.Core;
using System.Text.Json;

namespace Package.HealthCheck.ML;

/// <summary>
/// Serviço de análise preditiva que analisa padrões históricos para prever falhas
/// e sugerir ações de auto-healing baseado em aprendizado de máquina.
/// </summary>
public class PredictiveHealthAnalysis : IHealthCheck
{
    private readonly ILogger<PredictiveHealthAnalysis> _logger;
    private readonly IOptions<PredictiveAnalysisOptions> _options;
    private readonly IHealthHistoryRepository _historyRepository;
    private readonly IAlertService _alertService;

    public PredictiveHealthAnalysis(
        ILogger<PredictiveHealthAnalysis> logger,
        IOptions<PredictiveAnalysisOptions> options,
        IHealthHistoryRepository historyRepository,
        IAlertService alertService)
    {
        _logger = logger;
        _options = options;
        _historyRepository = historyRepository;
        _alertService = alertService;
    }

    /// <summary>
    /// Executa o HealthCheck de análise preditiva.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executando análise preditiva de saúde");

            // Obter histórico de saúde
            var healthHistory = await _historyRepository.GetHealthHistoryAsync(
                TimeSpan.FromHours(_options.Value.AnalysisWindowHours),
                cancellationToken);

            if (!healthHistory.Any())
            {
                return HealthCheckResult.Healthy(
                    description: "Análise preditiva não disponível - histórico insuficiente",
                    data: new Dictionary<string, object>
                    {
                        ["reason"] = "insufficient_history",
                        ["available_data_points"] = 0
                    });
            }

            // Analisar padrões e fazer previsões
            var predictions = await AnalyzeHealthPatterns(healthHistory, cancellationToken);
            
            // Verificar se há previsões críticas
            var criticalPredictions = predictions.Where(p => p.FailureProbability > _options.Value.CriticalThreshold).ToList();
            
            if (criticalPredictions.Any())
            {
                // Enviar alertas para previsões críticas
                await SendCriticalAlerts(criticalPredictions, cancellationToken);
                
                var data = new Dictionary<string, object>
                {
                    ["critical_predictions"] = criticalPredictions.Count,
                    ["total_predictions"] = predictions.Count,
                    ["highest_failure_probability"] = criticalPredictions.Max(p => p.FailureProbability),
                    ["predicted_failures"] = criticalPredictions.Select(p => new
                    {
                        component = p.ComponentName,
                        probability = p.FailureProbability,
                        timeframe = p.TimeFrame,
                        confidence = p.Confidence
                    })
                };

                return HealthCheckResult.Unhealthy(
                    description: $"Previsões críticas detectadas: {criticalPredictions.Count} componentes em risco",
                    data: data);
            }

            // Verificar previsões de degradação
            var degradationPredictions = predictions.Where(p => 
                p.FailureProbability > _options.Value.DegradationThreshold && 
                p.FailureProbability <= _options.Value.CriticalThreshold).ToList();

            var status = HealthStatus.Healthy;
            var description = "Análise preditiva indica sistema saudável";

            if (degradationPredictions.Any())
            {
                status = HealthStatus.Degraded;
                description = $"Degradação prevista em {degradationPredictions.Count} componentes";
            }

            var healthyData = new Dictionary<string, object>
            {
                ["total_predictions"] = predictions.Count,
                ["critical_predictions"] = criticalPredictions.Count,
                ["degradation_predictions"] = degradationPredictions.Count,
                ["healthy_predictions"] = predictions.Count - criticalPredictions.Count - degradationPredictions.Count,
                ["analysis_confidence"] = CalculateOverallConfidence(predictions),
                ["next_analysis"] = DateTime.UtcNow.AddMinutes(_options.Value.AnalysisIntervalMinutes)
            };

            return new HealthCheckResult(status, description, data: healthyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante análise preditiva de saúde");
            
            var errorData = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["error_type"] = ex.GetType().Name,
                ["last_analysis"] = DateTime.UtcNow
            };

            return HealthCheckResult.Unhealthy(
                description: "Erro durante análise preditiva",
                exception: ex,
                data: errorData);
        }
    }

    /// <summary>
    /// Analisa padrões de saúde e faz previsões.
    /// </summary>
    private async Task<List<HealthPrediction>> AnalyzeHealthPatterns(
        IEnumerable<HealthHistoryEntry> history,
        CancellationToken cancellationToken)
    {
        var predictions = new List<HealthPrediction>();
        var components = history.Select(h => h.ComponentName).Distinct();

        foreach (var component in components)
        {
            var componentHistory = history.Where(h => h.ComponentName == component).ToList();
            
            if (componentHistory.Count < _options.Value.MinimumDataPoints)
            {
                continue; // Dados insuficientes para análise
            }

            var prediction = await PredictComponentHealth(component, componentHistory, cancellationToken);
            if (prediction != null)
            {
                predictions.Add(prediction);
            }
        }

        return predictions;
    }

    /// <summary>
    /// Faz previsão de saúde para um componente específico.
    /// </summary>
    private async Task<HealthPrediction?> PredictComponentHealth(
        string componentName,
        List<HealthHistoryEntry> history,
        CancellationToken cancellationToken)
    {
        try
        {
            // Ordenar histórico por timestamp
            var orderedHistory = history.OrderBy(h => h.Timestamp).ToList();
            
            // Calcular métricas básicas
            var failureRate = CalculateFailureRate(orderedHistory);
            var trend = CalculateHealthTrend(orderedHistory);
            var seasonality = DetectSeasonality(orderedHistory);
            var anomalies = DetectAnomalies(orderedHistory);

            // Aplicar modelo de ML (simplificado para demonstração)
            var failureProbability = CalculateFailureProbability(failureRate, trend, seasonality, anomalies);
            var confidence = CalculatePredictionConfidence(orderedHistory.Count, anomalies.Count);
            var timeFrame = PredictTimeFrame(failureProbability, trend);

            var prediction = new HealthPrediction
            {
                ComponentName = componentName,
                FailureProbability = failureProbability,
                Confidence = confidence,
                TimeFrame = timeFrame,
                AnalysisTimestamp = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>
                {
                    ["failure_rate"] = failureRate,
                    ["trend"] = trend.ToString(),
                    ["seasonality_detected"] = seasonality,
                    ["anomalies_count"] = anomalies.Count,
                    ["data_points"] = orderedHistory.Count
                }
            };

            return prediction;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao prever saúde do componente: {ComponentName}", componentName);
            return null;
        }
    }

    /// <summary>
    /// Calcula a taxa de falha baseada no histórico.
    /// </summary>
    private double CalculateFailureRate(List<HealthHistoryEntry> history)
    {
        var totalChecks = history.Count;
        var failures = history.Count(h => h.Status == HealthStatus.Unhealthy);
        
        return totalChecks > 0 ? (double)failures / totalChecks : 0.0;
    }

    /// <summary>
    /// Calcula a tendência de saúde.
    /// </summary>
    private HealthTrend CalculateHealthTrend(List<HealthHistoryEntry> history)
    {
        if (history.Count < 2) return HealthTrend.Stable;

        var recent = history.TakeLast(history.Count / 2).ToList();
        var older = history.Take(history.Count / 2).ToList();

        var recentFailureRate = CalculateFailureRate(recent);
        var olderFailureRate = CalculateFailureRate(older);

        var difference = recentFailureRate - olderFailureRate;

        if (Math.Abs(difference) < 0.05) return HealthTrend.Stable;
        return difference > 0 ? HealthTrend.Deteriorating : HealthTrend.Improving;
    }

    /// <summary>
    /// Detecta sazonalidade nos padrões de saúde.
    /// </summary>
    private bool DetectSeasonality(List<HealthHistoryEntry> history)
    {
        if (history.Count < 24) return false; // Dados insuficientes

        // Análise simplificada de sazonalidade por hora
        var hourlyGroups = history.GroupBy(h => h.Timestamp.Hour).ToList();
        var hourlyFailureRates = hourlyGroups.Select(g => CalculateFailureRate(g.ToList())).ToList();

        // Calcular variância entre horas
        var mean = hourlyFailureRates.Average();
        var variance = hourlyFailureRates.Select(r => Math.Pow(r - mean, 2)).Average();

        return variance > 0.01; // Threshold para detectar sazonalidade
    }

    /// <summary>
    /// Detecta anomalias no histórico de saúde.
    /// </summary>
    private List<HealthHistoryEntry> DetectAnomalies(List<HealthHistoryEntry> history)
    {
        var anomalies = new List<HealthHistoryEntry>();
        
        if (history.Count < 3) return anomalies;

        var failureRates = new List<double>();
        var windowSize = Math.Min(10, history.Count / 3);

        for (int i = 0; i <= history.Count - windowSize; i++)
        {
            var window = history.Skip(i).Take(windowSize).ToList();
            failureRates.Add(CalculateFailureRate(window));
        }

        var mean = failureRates.Average();
        var stdDev = Math.Sqrt(failureRates.Select(r => Math.Pow(r - mean, 2)).Average());

        // Detectar valores que estão além de 2 desvios padrão
        var threshold = mean + (2 * stdDev);
        
        for (int i = 0; i <= history.Count - windowSize; i++)
        {
            var window = history.Skip(i).Take(windowSize).ToList();
            var failureRate = CalculateFailureRate(window);
            
            if (failureRate > threshold)
            {
                anomalies.AddRange(window);
            }
        }

        return anomalies.Distinct().ToList();
    }

    /// <summary>
    /// Calcula a probabilidade de falha baseada em múltiplos fatores.
    /// </summary>
    private double CalculateFailureProbability(
        double failureRate,
        HealthTrend trend,
        bool seasonality,
        List<HealthHistoryEntry> anomalies)
    {
        var baseProbability = failureRate;
        
        // Ajustar baseado na tendência
        var trendMultiplier = trend switch
        {
            HealthTrend.Deteriorating => 1.5,
            HealthTrend.Improving => 0.7,
            _ => 1.0
        };

        // Ajustar baseado na sazonalidade
        var seasonalityMultiplier = seasonality ? 1.2 : 1.0;

        // Ajustar baseado nas anomalias
        var anomalyMultiplier = anomalies.Count > 0 ? 1.0 + (anomalies.Count * 0.1) : 1.0;

        var adjustedProbability = baseProbability * trendMultiplier * seasonalityMultiplier * anomalyMultiplier;
        
        return Math.Min(adjustedProbability, 1.0); // Cap em 100%
    }

    /// <summary>
    /// Calcula a confiança da previsão.
    /// </summary>
    private double CalculatePredictionConfidence(int dataPoints, int anomalies)
    {
        var baseConfidence = Math.Min(dataPoints / 100.0, 1.0); // Máximo 100% com 100+ pontos
        
        // Reduzir confiança se houver muitas anomalias
        var anomalyPenalty = Math.Min(anomalies * 0.1, 0.3);
        
        return Math.Max(baseConfidence - anomalyPenalty, 0.1); // Mínimo 10%
    }

    /// <summary>
    /// Prevê o timeframe para uma possível falha.
    /// </summary>
    private TimeSpan PredictTimeFrame(double failureProbability, HealthTrend trend)
    {
        if (failureProbability < 0.3) return TimeSpan.FromHours(24);
        if (failureProbability < 0.6) return TimeSpan.FromHours(12);
        if (failureProbability < 0.8) return TimeSpan.FromHours(6);
        
        return trend == HealthTrend.Deteriorating ? TimeSpan.FromHours(1) : TimeSpan.FromHours(3);
    }

    /// <summary>
    /// Calcula a confiança geral da análise.
    /// </summary>
    private double CalculateOverallConfidence(List<HealthPrediction> predictions)
    {
        if (!predictions.Any()) return 0.0;
        
        var totalConfidence = predictions.Sum(p => p.Confidence);
        return totalConfidence / predictions.Count;
    }

    /// <summary>
    /// Envia alertas para previsões críticas.
    /// </summary>
    private async Task SendCriticalAlerts(
        List<HealthPrediction> criticalPredictions,
        CancellationToken cancellationToken)
    {
        foreach (var prediction in criticalPredictions)
        {
            var alert = new HealthAlert
            {
                Type = AlertType.Critical,
                Component = prediction.ComponentName,
                Message = $"Falha prevista com {prediction.FailureProbability:P1} de probabilidade em {prediction.TimeFrame.TotalHours:F1} horas",
                Severity = AlertSeverity.High,
                Timestamp = DateTime.UtcNow,
                Data = prediction.Metrics
            };

            await _alertService.SendAlertAsync(alert, cancellationToken);
            
            _logger.LogWarning(
                "Alerta crítico enviado para componente {ComponentName}: {Message}",
                prediction.ComponentName,
                alert.Message);
        }
    }
}

/// <summary>
/// Opções de configuração para análise preditiva.
/// </summary>
public class PredictiveAnalysisOptions
{
    /// <summary>
    /// Janela de análise em horas.
    /// </summary>
    public int AnalysisWindowHours { get; set; } = 24;

    /// <summary>
    /// Intervalo entre análises em minutos.
    /// </summary>
    public int AnalysisIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Número mínimo de pontos de dados para análise.
    /// </summary>
    public int MinimumDataPoints { get; set; } = 10;

    /// <summary>
    /// Threshold para considerar previsão como degradação.
    /// </summary>
    public double DegradationThreshold { get; set; } = 0.3;

    /// <summary>
    /// Threshold para considerar previsão como crítica.
    /// </summary>
    public double CriticalThreshold { get; set; } = 0.7;
}

/// <summary>
/// Previsão de saúde de um componente.
/// </summary>
public class HealthPrediction
{
    /// <summary>
    /// Nome do componente.
    /// </summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Probabilidade de falha (0.0 a 1.0).
    /// </summary>
    public double FailureProbability { get; set; }

    /// <summary>
    /// Confiança da previsão (0.0 a 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Timeframe previsto para a falha.
    /// </summary>
    public TimeSpan TimeFrame { get; set; }

    /// <summary>
    /// Timestamp da análise.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; }

    /// <summary>
    /// Métricas utilizadas na previsão.
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Tendência de saúde.
/// </summary>
public enum HealthTrend
{
    /// <summary>
    /// Saúde estável.
    /// </summary>
    Stable,

    /// <summary>
    /// Saúde melhorando.
    /// </summary>
    Improving,

    /// <summary>
    /// Saúde deteriorando.
    /// </summary>
    Deteriorating
}

/// <summary>
/// Interface para repositório de histórico de saúde.
/// </summary>
public interface IHealthHistoryRepository
{
    /// <summary>
    /// Obtém histórico de saúde para um período específico.
    /// </summary>
    Task<IEnumerable<HealthHistoryEntry>> GetHealthHistoryAsync(
        TimeSpan period,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface para serviço de alertas.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Envia um alerta.
    /// </summary>
    Task SendAlertAsync(HealthAlert alert, CancellationToken cancellationToken = default);
}

/// <summary>
/// Entrada do histórico de saúde.
/// </summary>
public class HealthHistoryEntry
{
    /// <summary>
    /// Nome do componente.
    /// </summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Status de saúde.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Timestamp da verificação.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Duração da verificação.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Dados adicionais.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Alerta de saúde.
/// </summary>
public class HealthAlert
{
    /// <summary>
    /// Tipo do alerta.
    /// </summary>
    public AlertType Type { get; set; }

    /// <summary>
    /// Componente afetado.
    /// </summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem do alerta.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severidade do alerta.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Timestamp do alerta.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Dados adicionais.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Tipo de alerta.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Alerta informativo.
    /// </summary>
    Info,

    /// <summary>
    /// Alerta de aviso.
    /// </summary>
    Warning,

    /// <summary>
    /// Alerta crítico.
    /// </summary>
    Critical
}

/// <summary>
/// Severidade do alerta.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Severidade baixa.
    /// </summary>
    Low,

    /// <summary>
    /// Severidade média.
    /// </summary>
    Medium,

    /// <summary>
    /// Severidade alta.
    /// </summary>
    High
}
