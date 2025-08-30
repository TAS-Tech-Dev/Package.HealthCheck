using System.ComponentModel;

namespace Package.HealthCheck.Attributes;

/// <summary>
/// Atributo para marcar classes que precisam de HealthChecks específicos.
/// Permite configuração declarativa e auto-documentada.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
public class HealthCheckAttribute : Attribute
{
    /// <summary>
    /// Nome do HealthCheck.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Tags para categorização do HealthCheck.
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Tipo de HealthCheck a ser criado.
    /// </summary>
    public HealthCheckType Type { get; }

    /// <summary>
    /// Timeout em segundos para o HealthCheck.
    /// </summary>
    public int TimeoutSeconds { get; }

    /// <summary>
    /// Indica se o HealthCheck é crítico para a aplicação.
    /// </summary>
    public bool IsCritical { get; }

    /// <summary>
    /// Configurações específicas do HealthCheck em formato JSON.
    /// </summary>
    public string? Configuration { get; }

    /// <summary>
    /// Inicializa uma nova instância do atributo HealthCheck.
    /// </summary>
    /// <param name="name">Nome do HealthCheck.</param>
    /// <param name="type">Tipo do HealthCheck.</param>
    /// <param name="tags">Tags para categorização.</param>
    /// <param name="timeoutSeconds">Timeout em segundos.</param>
    /// <param name="isCritical">Se é crítico para a aplicação.</param>
    /// <param name="configuration">Configurações específicas em JSON.</param>
    public HealthCheckAttribute(
        string name,
        HealthCheckType type = HealthCheckType.Custom,
        string[]? tags = null,
        int timeoutSeconds = 30,
        bool isCritical = false,
        string? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null, empty, or whitespace.", nameof(name));
        
        Name = name;
        Type = type;
        Tags = tags ?? new[] { "custom" };
        TimeoutSeconds = timeoutSeconds;
        IsCritical = isCritical;
        Configuration = configuration;
    }
}

/// <summary>
/// Tipos de HealthChecks suportados pelo atributo.
/// </summary>
public enum HealthCheckType
{
    /// <summary>
    /// HealthCheck customizado implementado pelo usuário.
    /// </summary>
    [Description("Custom")]
    Custom,

    /// <summary>
    /// HealthCheck para banco de dados.
    /// </summary>
    [Description("Database")]
    Database,

    /// <summary>
    /// HealthCheck para serviço HTTP.
    /// </summary>
    [Description("Http")]
    Http,

    /// <summary>
    /// HealthCheck para fila de mensagens.
    /// </summary>
    [Description("MessageQueue")]
    MessageQueue,

    /// <summary>
    /// HealthCheck para cache.
    /// </summary>
    [Description("Cache")]
    Cache,

    /// <summary>
    /// HealthCheck para sistema de arquivos.
    /// </summary>
    [Description("FileSystem")]
    FileSystem,

    /// <summary>
    /// HealthCheck para recursos de memória.
    /// </summary>
    [Description("Memory")]
    Memory,

    /// <summary>
    /// HealthCheck para CPU.
    /// </summary>
    [Description("Cpu")]
    Cpu,

    /// <summary>
    /// HealthCheck para rede.
    /// </summary>
    [Description("Network")]
    Network
}
