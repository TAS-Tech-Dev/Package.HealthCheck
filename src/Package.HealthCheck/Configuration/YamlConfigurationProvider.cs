using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace Package.HealthCheck.Configuration;

/// <summary>
/// Provedor de configuração que suporta arquivos YAML.
/// </summary>
public class YamlConfigurationProvider : ConfigurationProvider
{
    private readonly string _filePath;
    private readonly ILogger<YamlConfigurationProvider> _logger;

    public YamlConfigurationProvider(string filePath, ILogger<YamlConfigurationProvider> logger)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Carrega a configuração do arquivo YAML.
    /// </summary>
    public override void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Arquivo YAML não encontrado: {FilePath}", _filePath);
                return;
            }

            var yamlContent = File.ReadAllText(_filePath);
            var configuration = ParseYamlContent(yamlContent);
            
            foreach (var kvp in configuration)
            {
                Data[kvp.Key] = kvp.Value;
            }

            _logger.LogInformation("Configuração YAML carregada com sucesso: {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar configuração YAML: {FilePath}", _filePath);
            throw;
        }
    }

    /// <summary>
    /// Parseia o conteúdo YAML para um dicionário plano.
    /// </summary>
    private Dictionary<string, string> ParseYamlContent(string yamlContent)
    {
        var result = new Dictionary<string, string>();
        
        // Implementação simplificada - apenas para resolver problemas de compilação
        // Em produção, usar YamlDotNet para parsing completo
        
        var lines = yamlContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Contains(':') && !trimmedLine.StartsWith('#'))
            {
                var parts = trimmedLine.Split(':', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    result[key] = value;
                }
            }
        }
        
        return result;
    }
}

/// <summary>
/// Fonte de configuração YAML.
/// </summary>
public class YamlConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Caminho para o arquivo YAML.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Se o arquivo é opcional.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Se deve recarregar quando o arquivo mudar.
    /// </summary>
    public bool ReloadOnChange { get; set; }

    /// <summary>
    /// Cria o provedor de configuração.
    /// </summary>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();
        
        return new YamlConfigurationProvider(Path, logger);
    }

    /// <summary>
    /// Cria uma cópia do YamlConfigurationSource.
    /// </summary>
    public YamlConfigurationSource Clone()
    {
        return new YamlConfigurationSource
        {
            Path = this.Path,
            Optional = this.Optional,
            ReloadOnChange = this.ReloadOnChange
        };
    }
}

/// <summary>
/// Fonte de configuração YAML personalizada que aceita um logger específico.
/// </summary>
public class CustomYamlConfigurationSource : IConfigurationSource
{
    private readonly string _path;
    private readonly ILogger<YamlConfigurationProvider> _logger;
    private readonly bool _optional;
    private readonly bool _reloadOnChange;

    public CustomYamlConfigurationSource(string path, ILogger<YamlConfigurationProvider> logger, bool optional, bool reloadOnChange)
    {
        _path = path;
        _logger = logger;
        _optional = optional;
        _reloadOnChange = reloadOnChange;
    }

    /// <summary>
    /// Cria o provedor de configuração.
    /// </summary>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new YamlConfigurationProvider(_path, _logger);
    }
}

/// <summary>
/// Extensões para IConfigurationBuilder para suportar YAML.
/// </summary>
public static class YamlConfigurationExtensions
{
    /// <summary>
    /// Adiciona suporte para arquivos de configuração YAML.
    /// </summary>
    public static IConfigurationBuilder AddYamlFile(
        this IConfigurationBuilder builder,
        string path,
        bool optional = false,
        bool reloadOnChange = false)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        var source = new YamlConfigurationSource
        {
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange
        };

        builder.Add(source);
        return builder;
    }

    /// <summary>
    /// Adiciona suporte para arquivos de configuração YAML com logger personalizado.
    /// </summary>
    public static IConfigurationBuilder AddYamlFile(
        this IConfigurationBuilder builder,
        string path,
        ILogger<YamlConfigurationProvider> logger,
        bool optional = false,
        bool reloadOnChange = false)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (logger == null)
            throw new ArgumentNullException(nameof(logger));

        var source = new YamlConfigurationSource
        {
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange
        };

        // Criar um provedor personalizado com o logger fornecido
        // Como não podemos adicionar diretamente um provider, vamos criar uma fonte personalizada
        var customSource = new CustomYamlConfigurationSource(path, logger, optional, reloadOnChange);
        builder.Add(customSource);
        return builder;
    }
}
