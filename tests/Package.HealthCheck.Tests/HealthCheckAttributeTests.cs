using FluentAssertions;
using Package.HealthCheck.Attributes;
using Xunit;

namespace Package.HealthCheck.Tests;

public class HealthCheckAttributeTests
{
    [Fact]
    public void HealthCheckAttribute_Constructor_WithValidParameters_ShouldCreateAttribute()
    {
        // Arrange
        var name = "test-health-check";
        var type = HealthCheckType.Database;
        var tags = new[] { "test", "database" };
        var timeoutSeconds = 30;
        var isCritical = true;
        var configuration = "test-config";

        // Act
        var attribute = new HealthCheckAttribute(name, type, tags, timeoutSeconds, isCritical, configuration);

        // Assert
        attribute.Name.Should().Be(name);
        attribute.Type.Should().Be(type);
        attribute.Tags.Should().BeEquivalentTo(tags);
        attribute.TimeoutSeconds.Should().Be(timeoutSeconds);
        attribute.IsCritical.Should().Be(isCritical);
        attribute.Configuration.Should().Be(configuration);
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithMinimalParameters_ShouldUseDefaultValues()
    {
        // Arrange
        var name = "test-health-check";

        // Act
        var attribute = new HealthCheckAttribute(name);

        // Assert
        attribute.Name.Should().Be(name);
        attribute.Type.Should().Be(HealthCheckType.Custom);
        attribute.Tags.Should().BeEquivalentTo(new[] { "custom" });
        attribute.TimeoutSeconds.Should().Be(30);
        attribute.IsCritical.Should().BeFalse();
        attribute.Configuration.Should().BeNull();
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithNullName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new HealthCheckAttribute(null!);
        action.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new HealthCheckAttribute("");
        action.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new HealthCheckAttribute("   ");
        action.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithNullTags_ShouldUseDefaultTags()
    {
        // Arrange
        var name = "test-health-check";

        // Act
        var attribute = new HealthCheckAttribute(name, tags: null);

        // Assert
        attribute.Tags.Should().BeEquivalentTo(new[] { "custom" });
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithEmptyTags_ShouldUseEmptyTags()
    {
        // Arrange
        var name = "test-health-check";
        var tags = new string[0];

        // Act
        var attribute = new HealthCheckAttribute(name, tags: tags);

        // Assert
        attribute.Tags.Should().BeEmpty();
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithCustomTimeout_ShouldSetTimeout()
    {
        // Arrange
        var name = "test-health-check";
        var timeoutSeconds = 60;

        // Act
        var attribute = new HealthCheckAttribute(name, timeoutSeconds: timeoutSeconds);

        // Assert
        attribute.TimeoutSeconds.Should().Be(timeoutSeconds);
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithCustomType_ShouldSetType()
    {
        // Arrange
        var name = "test-health-check";
        var type = HealthCheckType.Http;

        // Act
        var attribute = new HealthCheckAttribute(name, type);

        // Assert
        attribute.Type.Should().Be(type);
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithCriticalTrue_ShouldSetIsCritical()
    {
        // Arrange
        var name = "test-health-check";

        // Act
        var attribute = new HealthCheckAttribute(name, isCritical: true);

        // Assert
        attribute.IsCritical.Should().BeTrue();
    }

    [Fact]
    public void HealthCheckAttribute_Constructor_WithConfiguration_ShouldSetConfiguration()
    {
        // Arrange
        var name = "test-health-check";
        var configuration = "custom-config";

        // Act
        var attribute = new HealthCheckAttribute(name, configuration: configuration);

        // Assert
        attribute.Configuration.Should().Be(configuration);
    }

    [Theory]
    [InlineData(HealthCheckType.Custom)]
    [InlineData(HealthCheckType.Database)]
    [InlineData(HealthCheckType.Http)]
    [InlineData(HealthCheckType.MessageQueue)]
    [InlineData(HealthCheckType.Cache)]
    [InlineData(HealthCheckType.FileSystem)]
    [InlineData(HealthCheckType.Memory)]
    [InlineData(HealthCheckType.Cpu)]
    [InlineData(HealthCheckType.Network)]
    public void HealthCheckAttribute_AllHealthCheckTypes_ShouldBeSupported(HealthCheckType type)
    {
        // Arrange
        var name = "test-health-check";

        // Act
        var attribute = new HealthCheckAttribute(name, type);

        // Assert
        attribute.Type.Should().Be(type);
    }

    [Fact]
    public void HealthCheckAttribute_Usage_ShouldAllowMultipleInstances()
    {
        // Arrange & Act
        var attribute1 = new HealthCheckAttribute("health-check-1", HealthCheckType.Database);
        var attribute2 = new HealthCheckAttribute("health-check-2", HealthCheckType.Http);

        // Assert
        attribute1.Name.Should().Be("health-check-1");
        attribute1.Type.Should().Be(HealthCheckType.Database);
        attribute2.Name.Should().Be("health-check-2");
        attribute2.Type.Should().Be(HealthCheckType.Http);
    }

    [Fact]
    public void HealthCheckAttribute_Usage_ShouldAllowComplexConfiguration()
    {
        // Arrange
        var name = "complex-health-check";
        var type = HealthCheckType.Database;
        var tags = new[] { "critical", "database", "production" };
        var timeoutSeconds = 120;
        var isCritical = true;
        var configuration = "production-db-config";

        // Act
        var attribute = new HealthCheckAttribute(name, type, tags, timeoutSeconds, isCritical, configuration);

        // Assert
        attribute.Name.Should().Be(name);
        attribute.Type.Should().Be(type);
        attribute.Tags.Should().BeEquivalentTo(tags);
        attribute.TimeoutSeconds.Should().Be(timeoutSeconds);
        attribute.IsCritical.Should().Be(isCritical);
        attribute.Configuration.Should().Be(configuration);
    }

    [Fact]
    public void HealthCheckAttribute_Usage_ShouldAllowZeroTimeout()
    {
        // Arrange
        var name = "zero-timeout-check";
        var timeoutSeconds = 0;

        // Act
        var attribute = new HealthCheckAttribute(name, timeoutSeconds: timeoutSeconds);

        // Assert
        attribute.TimeoutSeconds.Should().Be(0);
    }

    [Fact]
    public void HealthCheckAttribute_Usage_ShouldAllowNegativeTimeout()
    {
        // Arrange
        var name = "negative-timeout-check";
        var timeoutSeconds = -1;

        // Act
        var attribute = new HealthCheckAttribute(name, timeoutSeconds: timeoutSeconds);

        // Assert
        attribute.TimeoutSeconds.Should().Be(-1);
    }

    [Fact]
    public void HealthCheckAttribute_Usage_ShouldAllowVeryLargeTimeout()
    {
        // Arrange
        var name = "large-timeout-check";
        var timeoutSeconds = int.MaxValue;

        // Act
        var attribute = new HealthCheckAttribute(name, timeoutSeconds: timeoutSeconds);

        // Assert
        attribute.TimeoutSeconds.Should().Be(int.MaxValue);
    }

    [Fact]
    public void HealthCheckAttribute_Usage_ShouldAllowEmptyConfiguration()
    {
        // Arrange
        var name = "empty-config-check";
        var configuration = "";

        // Act
        var attribute = new HealthCheckAttribute(name, configuration: configuration);

        // Assert
        attribute.Configuration.Should().Be("");
    }

    [Fact]
    public void HealthCheckAttribute_Usage_ShouldAllowWhitespaceConfiguration()
    {
        // Arrange
        var name = "whitespace-config-check";
        var configuration = "   ";

        // Act
        var attribute = new HealthCheckAttribute(name, configuration: configuration);

        // Assert
        attribute.Configuration.Should().Be("   ");
    }
}

public class HealthCheckTypeEnumTests
{
    [Fact]
    public void HealthCheckType_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<HealthCheckType>().Should().Contain(HealthCheckType.Custom);
        Enum.GetValues<HealthCheckType>().Should().Contain(HealthCheckType.Database);
        Enum.GetValues<HealthCheckType>().Should().Contain(HealthCheckType.Http);
        Enum.GetValues<HealthCheckType>().Should().Contain(HealthCheckType.MessageQueue);
        Enum.GetValues<HealthCheckType>().Should().Contain(HealthCheckType.Cache);
        Enum.GetValues<HealthCheckType>().Should().Contain(HealthCheckType.FileSystem);
        Enum.GetValues<HealthCheckType>().Should().Contain(HealthCheckType.Memory);
        Enum.GetValues<HealthCheckType>().Should().Contain(HealthCheckType.Cpu);
        Enum.GetValues<HealthCheckType>().Should().Contain(HealthCheckType.Network);
    }

    [Fact]
    public void HealthCheckType_ShouldHaveExpectedCount()
    {
        // Assert
        Enum.GetValues<HealthCheckType>().Should().HaveCount(10);
    }

    [Theory]
    [InlineData(HealthCheckType.Custom, "Custom")]
    [InlineData(HealthCheckType.Database, "Database")]
    [InlineData(HealthCheckType.Http, "Http")]
    [InlineData(HealthCheckType.MessageQueue, "MessageQueue")]
    [InlineData(HealthCheckType.Cache, "Cache")]
    [InlineData(HealthCheckType.FileSystem, "FileSystem")]
    [InlineData(HealthCheckType.Memory, "Memory")]
    [InlineData(HealthCheckType.Cpu, "Cpu")]
    [InlineData(HealthCheckType.Network, "Network")]
    public void HealthCheckType_ToString_ShouldReturnExpectedValue(HealthCheckType type, string expected)
    {
        // Act & Assert
        type.ToString().Should().Be(expected);
    }
}
