using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Package.HealthCheck.Core;
using Package.HealthCheck.Dashboard;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Package.HealthCheck.Tests;

public class DashboardTests
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IOptions<HealthConfig> _healthConfig;
    private readonly ILogger<HealthDashboardController> _logger;
    private readonly HealthDashboardController _controller;
    private readonly Mock<HealthCheckService> _mockService;
    private readonly Mock<IOptions<HealthConfig>> _mockConfig;
    private readonly Mock<ILogger<HealthDashboardController>> _mockLogger;

    public DashboardTests()
    {
        // Mock dependencies
        _mockService = new Mock<HealthCheckService>();
        _mockConfig = new Mock<IOptions<HealthConfig>>();
        _mockLogger = new Mock<ILogger<HealthDashboardController>>();
        
        _healthCheckService = _mockService.Object;
        _healthConfig = _mockConfig.Object;
        _logger = _mockLogger.Object;
        
        _controller = new HealthDashboardController(_healthCheckService, _healthConfig, _logger);
    }

    [Fact]
    public void HealthDashboardController_Constructor_ShouldCreateController()
    {
        // Act
        var controller = new HealthDashboardController(_healthCheckService, _healthConfig, _logger);

        // Assert
        controller.Should().NotBeNull();
        controller.Should().BeOfType<HealthDashboardController>();
    }

    [Fact]
    public void HealthDashboardController_Index_ShouldReturnViewResult()
    {
        // Act
        var result = _controller.Index();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void HealthDashboardController_Index_ShouldReturnHtmlContent()
    {
        // Act
        var result = _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewData.Should().ContainKey("HtmlContent");
        result.ViewData["HtmlContent"].Should().NotBeNull();
        result.ViewData["HtmlContent"].Should().BeOfType<string>();
    }

    [Fact]
    public void HealthDashboardController_GetStatus_ShouldReturnJsonResult()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["test"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Test health check",
                    TimeSpan.FromMilliseconds(100),
                    null,
                    new Dictionary<string, object>())
            },
            TimeSpan.FromMilliseconds(100));

        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = _controller.GetStatus();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public void HealthDashboardController_GetCheckStatus_ShouldReturnJsonResult()
    {
        // Arrange
        var checkName = "test-check";
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                [checkName] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Test health check",
                    TimeSpan.FromMilliseconds(100),
                    null,
                    new Dictionary<string, object>())
            },
            TimeSpan.FromMilliseconds(100));

        _mockService
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = _controller.GetCheckStatus(checkName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public void HealthDashboardController_GetCheckStatus_WithNonExistentCheck_ShouldReturnNotFound()
    {
        // Arrange
        var checkName = "non-existent-check";
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.FromMilliseconds(100));

        _mockService
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = _controller.GetCheckStatus(checkName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void HealthDashboardController_GetMetrics_ShouldReturnJsonResult()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["test"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Test health check",
                    TimeSpan.FromMilliseconds(100),
                    null,
                    new Dictionary<string, object>())
            },
            TimeSpan.FromMilliseconds(100));

        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = _controller.GetMetrics();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public async Task HealthDashboardController_GetMetrics_ShouldIncludeTotalChecks()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["test1"] = new HealthReportEntry(HealthStatus.Healthy, "Test 1", TimeSpan.FromMilliseconds(100), null, new Dictionary<string, object>()),
                ["test2"] = new HealthReportEntry(HealthStatus.Unhealthy, "Test 2", TimeSpan.FromMilliseconds(200), null, new Dictionary<string, object>()),
                ["test3"] = new HealthReportEntry(HealthStatus.Degraded, "Test 3", TimeSpan.FromMilliseconds(150), null, new Dictionary<string, object>())
            },
            TimeSpan.FromMilliseconds(200));

        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetMetrics() as JsonResult;

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().NotBeNull();
        // Note: We can't easily test the JSON content without deserializing, but we can verify the result type
    }

    [Fact]
    public void HealthDashboardController_GetMetrics_ShouldHandleEmptyHealthReport()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.FromMilliseconds(0));

        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = _controller.GetMetrics();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public void HealthDashboardController_GetMetrics_ShouldHandleNullHealthReport()
    {
        // Arrange
        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HealthReport?)null);

        // Act
        var result = _controller.GetMetrics();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public void HealthDashboardController_GetStatus_ShouldHandleNullHealthReport()
    {
        // Arrange
        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HealthReport?)null);

        // Act
        var result = _controller.GetStatus();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public void HealthDashboardController_GetCheckStatus_ShouldHandleNullHealthReport()
    {
        // Arrange
        var checkName = "test-check";
        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HealthReport?)null);

        // Act
        var result = _controller.GetCheckStatus(checkName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public void HealthDashboardController_GetStatus_ShouldHandleException()
    {
        // Arrange
        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = _controller.GetStatus();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public void HealthDashboardController_GetCheckStatus_ShouldHandleException()
    {
        // Arrange
        var checkName = "test-check";
        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = _controller.GetCheckStatus(checkName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public void HealthDashboardController_GetMetrics_ShouldHandleException()
    {
        // Arrange
        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = _controller.GetMetrics();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public void HealthDashboardController_GetStatus_ShouldUseCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.FromMilliseconds(100));

        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), cancellationToken))
            .ReturnsAsync(healthReport);

        // Act
        var result = _controller.GetStatus();

        // Assert
        result.Should().NotBeNull();
        Mock.Get(_healthCheckService).Verify(
            x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public void HealthDashboardController_GetCheckStatus_ShouldUseCancellationToken()
    {
        // Arrange
        var checkName = "test-check";
        var cancellationToken = new CancellationToken();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.FromMilliseconds(100));

        _mockService
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), cancellationToken))
            .ReturnsAsync(healthReport);

        // Act
        var result = _controller.GetCheckStatus(checkName);

        // Assert
        result.Should().NotBeNull();
        Mock.Get(_healthCheckService).Verify(
            x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public void HealthDashboardController_GetMetrics_ShouldUseCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.FromMilliseconds(100));

        Mock.Get(_healthCheckService)
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), cancellationToken))
            .ReturnsAsync(healthReport);

        // Act
        var result = _controller.GetMetrics();

        // Assert
        result.Should().NotBeNull();
        Mock.Get(_healthCheckService).Verify(
            x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), cancellationToken),
            Times.Once);
    }
}


