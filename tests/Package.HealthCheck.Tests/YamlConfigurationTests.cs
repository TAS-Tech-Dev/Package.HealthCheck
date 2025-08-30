using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Package.HealthCheck.Configuration;
using Xunit;

namespace Package.HealthCheck.Tests;

public class YamlConfigurationTests
{
    [Fact]
    public void YamlConfigurationSource_Constructor_ShouldCreateSource()
    {
        // Act
        var source = new YamlConfigurationSource();

        // Assert
        source.Should().NotBeNull();
        source.Path.Should().BeNull();
        source.Optional.Should().BeFalse();
        source.ReloadOnChange.Should().BeFalse();
    }

    [Fact]
    public void YamlConfigurationSource_Build_ShouldReturnProvider()
    {
        // Arrange
        var source = new YamlConfigurationSource();

        // Act
        var provider = source.Build(new ConfigurationBuilder());

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<YamlConfigurationProvider>();
    }

    [Fact]
    public void YamlConfigurationProvider_Constructor_ShouldCreateProvider()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();

        // Act
        var provider = new YamlConfigurationProvider("test.yaml", logger);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_ShouldAddSourceToBuilder()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile("test.yaml");

        // Assert
        result.Should().BeSameAs(builder);
        builder.Sources.Should().ContainSingle(s => s is YamlConfigurationSource);
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithOptional_ShouldSetOptionalFlag()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile("test.yaml", optional: true);

        // Assert
        result.Should().BeSameAs(builder);
        var source = builder.Sources.OfType<YamlConfigurationSource>().Single();
        source.Optional.Should().BeTrue();
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithReloadOnChange_ShouldSetReloadFlag()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile("test.yaml", reloadOnChange: true);

        // Assert
        result.Should().BeSameAs(builder);
        var source = builder.Sources.OfType<YamlConfigurationSource>().Single();
        source.ReloadOnChange.Should().BeTrue();
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithLogger_ShouldAddSourceToBuilder()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();

        // Act
        var result = builder.AddYamlFile("test.yaml", logger);

        // Assert
        result.Should().BeSameAs(builder);
        builder.Sources.Should().ContainSingle(s => s is YamlConfigurationSource);
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithLoggerAndOptional_ShouldSetOptionalFlag()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();

        // Act
        var result = builder.AddYamlFile("test.yaml", logger, optional: true);

        // Assert
        result.Should().BeSameAs(builder);
        var source = builder.Sources.OfType<YamlConfigurationSource>().Single();
        source.Optional.Should().BeTrue();
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithLoggerAndReloadOnChange_ShouldSetReloadFlag()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();

        // Act
        var result = builder.AddYamlFile("test.yaml", logger, reloadOnChange: true);

        // Assert
        result.Should().BeSameAs(builder);
        var source = builder.Sources.OfType<YamlConfigurationSource>().Single();
        source.ReloadOnChange.Should().BeTrue();
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithAllParameters_ShouldSetAllFlags()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();

        // Act
        var result = builder.AddYamlFile("test.yaml", logger, optional: true, reloadOnChange: true);

        // Assert
        result.Should().BeSameAs(builder);
        var source = builder.Sources.OfType<YamlConfigurationSource>().Single();
        source.Optional.Should().BeTrue();
        source.ReloadOnChange.Should().BeTrue();
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithNullPath_ShouldAddSourceWithNullPath()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile(null!);

        // Assert
        result.Should().BeSameAs(builder);
        var source = builder.Sources.OfType<YamlConfigurationSource>().Single();
        source.Path.Should().BeNull();
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithEmptyPath_ShouldAddSourceWithEmptyPath()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile("");

        // Assert
        result.Should().BeSameAs(builder);
        var source = builder.Sources.OfType<YamlConfigurationSource>().Single();
        source.Path.Should().Be("");
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithWhitespacePath_ShouldAddSourceWithWhitespacePath()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile("   ");

        // Assert
        result.Should().BeSameAs(builder);
        var source = builder.Sources.OfType<YamlConfigurationSource>().Single();
        source.Path.Should().Be("   ");
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_WithNullLogger_ShouldAddSourceWithoutLogger()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile("test.yaml");

        // Assert
        result.Should().BeSameAs(builder);
        builder.Sources.Should().ContainSingle(s => s is YamlConfigurationSource);
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_ShouldAllowMethodChaining()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder
            .AddYamlFile("config1.yaml")
            .AddYamlFile("config2.yaml", optional: true)
            .AddYamlFile("config3.yaml", reloadOnChange: true);

        // Assert
        result.Should().BeSameAs(builder);
        builder.Sources.Should().HaveCount(3);
        builder.Sources.Should().AllBeOfType<YamlConfigurationSource>();
    }

    [Fact]
    public void YamlConfigurationExtensions_AddYamlFile_ShouldPreserveExistingSources()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string> { ["key"] = "value" });

        // Act
        var result = builder.AddYamlFile("test.yaml");

        // Assert
        result.Should().BeSameAs(builder);
        builder.Sources.Should().HaveCount(2);
        builder.Sources.Should().ContainSingle(s => s is YamlConfigurationSource);
    }

    [Fact]
    public void YamlConfigurationSource_Properties_ShouldBeSettable()
    {
        // Arrange
        var source = new YamlConfigurationSource();

        // Act
        source.Path = "custom.yaml";
        source.Optional = true;
        source.ReloadOnChange = true;

        // Assert
        source.Path.Should().Be("custom.yaml");
        source.Optional.Should().BeTrue();
        source.ReloadOnChange.Should().BeTrue();
    }

    [Fact]
    public void YamlConfigurationSource_Properties_ShouldBeGettable()
    {
        // Arrange
        var source = new YamlConfigurationSource
        {
            Path = "test.yaml",
            Optional = true,
            ReloadOnChange = true
        };

        // Assert
        source.Path.Should().Be("test.yaml");
        source.Optional.Should().BeTrue();
        source.ReloadOnChange.Should().BeTrue();
    }

    [Fact]
    public void YamlConfigurationSource_Clone_ShouldCreateNewInstance()
    {
        // Arrange
        var source = new YamlConfigurationSource
        {
            Path = "test.yaml",
            Optional = true,
            ReloadOnChange = true
        };

        // Act
        var clone = source.Clone();

        // Assert
        clone.Should().NotBeSameAs(source);
        clone.Should().BeOfType<YamlConfigurationSource>();
        clone.Path.Should().Be(source.Path);
        clone.Optional.Should().Be(source.Optional);
        clone.ReloadOnChange.Should().Be(source.ReloadOnChange);
    }

    [Fact]
    public void YamlConfigurationProvider_Load_ShouldNotThrowException()
    {
        // Arrange
        var source = new YamlConfigurationSource { Path = "nonexistent.yaml", Optional = true };
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();
        var provider = new YamlConfigurationProvider(source.Path, logger);

        // Act & Assert
        var action = () => provider.Load();
        action.Should().NotThrow();
    }

    [Fact]
    public void YamlConfigurationProvider_GetChildKeys_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        var source = new YamlConfigurationSource();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();
        var provider = new YamlConfigurationProvider(source.Path, logger);

        // Act
        var keys = provider.GetChildKeys(Enumerable.Empty<string>(), "");

        // Assert
        keys.Should().NotBeNull();
        keys.Should().BeEmpty();
    }

    [Fact]
    public void YamlConfigurationProvider_TryGet_ShouldReturnFalse()
    {
        // Arrange
        var source = new YamlConfigurationSource();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();
        var provider = new YamlConfigurationProvider(source.Path, logger);

        // Act
        var result = provider.TryGet("test", out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void YamlConfigurationProvider_Set_ShouldNotThrowException()
    {
        // Arrange
        var source = new YamlConfigurationSource();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<YamlConfigurationProvider>();
        var provider = new YamlConfigurationProvider(source.Path, logger);

        // Act & Assert
        var action = () => provider.Set("test", "value");
        action.Should().NotThrow();
    }
}
