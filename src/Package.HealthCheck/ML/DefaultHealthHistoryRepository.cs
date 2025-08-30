using Microsoft.Extensions.Logging;

namespace Package.HealthCheck.ML;

/// <summary>
/// Implementação padrão do repositório de histórico de saúde.
/// Armazena dados em memória para demonstração.
/// </summary>
public class DefaultHealthHistoryRepository : IHealthHistoryRepository
{
    private readonly ILogger<DefaultHealthHistoryRepository> _logger;
    private readonly List<HealthHistoryEntry> _history;
    private readonly object _lock = new object();

    public DefaultHealthHistoryRepository(ILogger<DefaultHealthHistoryRepository> logger)
    {
        _logger = logger;
        _history = new List<HealthHistoryEntry>();
    }

    /// <summary>
    /// Obtém histórico de saúde para um período específico.
    /// </summary>
    public Task<IEnumerable<HealthHistoryEntry>> GetHealthHistoryAsync(
        TimeSpan period,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(period);
        
        lock (_lock)
        {
            var result = _history
                .Where(h => h.Timestamp >= cutoffTime)
                .OrderByDescending(h => h.Timestamp)
                .ToList();

            _logger.LogDebug("Retornando {Count} entradas de histórico para período de {Period} horas", 
                result.Count, period.TotalHours);

            return Task.FromResult<IEnumerable<HealthHistoryEntry>>(result);
        }
    }

    /// <summary>
    /// Adiciona uma nova entrada ao histórico.
    /// </summary>
    public Task AddHealthEntryAsync(HealthHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        lock (_lock)
        {
            _history.Add(entry);
            
            // Manter apenas as últimas 1000 entradas para evitar crescimento excessivo
            if (_history.Count > 1000)
            {
                var toRemove = _history.Count - 1000;
                _history.RemoveRange(0, toRemove);
                _logger.LogDebug("Removidas {Count} entradas antigas do histórico", toRemove);
            }
        }

        _logger.LogDebug("Adicionada entrada de histórico para componente {ComponentName}", entry.ComponentName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Limpa entradas antigas do histórico.
    /// </summary>
    public Task CleanupOldEntriesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(maxAge);
        var removedCount = 0;

        lock (_lock)
        {
            removedCount = _history.RemoveAll(h => h.Timestamp < cutoffTime);
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Removidas {Count} entradas antigas do histórico (mais antigas que {MaxAge} horas)", 
                removedCount, maxAge.TotalHours);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Obtém estatísticas do histórico.
    /// </summary>
    public Task<HealthHistoryStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var stats = new HealthHistoryStats
            {
                TotalEntries = _history.Count,
                OldestEntry = _history.Any() ? _history.Min(h => h.Timestamp) : DateTime.UtcNow,
                NewestEntry = _history.Any() ? _history.Max(h => h.Timestamp) : DateTime.UtcNow,
                ComponentsCount = _history.Select(h => h.ComponentName).Distinct().Count(),
                AverageEntryAge = _history.Any() ? 
                    _history.Average(h => (DateTime.UtcNow - h.Timestamp).TotalMinutes) : 0
            };

            return Task.FromResult(stats);
        }
    }
}

/// <summary>
/// Estatísticas do histórico de saúde.
/// </summary>
public class HealthHistoryStats
{
    /// <summary>
    /// Total de entradas no histórico.
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Entrada mais antiga.
    /// </summary>
    public DateTime OldestEntry { get; set; }

    /// <summary>
    /// Entrada mais recente.
    /// </summary>
    public DateTime NewestEntry { get; set; }

    /// <summary>
    /// Número de componentes únicos.
    /// </summary>
    public int ComponentsCount { get; set; }

    /// <summary>
    /// Idade média das entradas em minutos.
    /// </summary>
    public double AverageEntryAge { get; set; }
}
