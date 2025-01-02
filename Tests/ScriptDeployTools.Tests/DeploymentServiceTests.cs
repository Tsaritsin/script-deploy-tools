using Microsoft.Extensions.Logging;
using Moq;

namespace ScriptDeployTools.Tests;

public class DeploymentServiceTests
{
    private readonly Mock<ILogger> _mockLogger = new();
    private readonly Mock<IDeploySource> _mockSource = new();
    private readonly Mock<IDeployTarget> _mockTarget = new();

    [Fact]
    public async Task ScriptAlreadyDeployed_DoesNotDeployAgain()
    {
        // Arrange
        var deployedScripts = new List<ScriptDeployed>
        {
            new("Script1") { ContentsHash = "hash1" }
        };

        var scriptsToDeploy = new Dictionary<string, Script>
        {
            { "Script1", new Script("Script1", "TestContent1") { Name = "Script1", ContentsHash = "hash1", CanRepeat = false } }
        };

        _mockTarget
            .Setup(t => t.GetDeployedScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployedScripts);

        _mockSource
            .Setup(s => s.GetScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptsToDeploy);

        var deploymentService = new DeploymentService(
            _mockLogger.Object,
            _mockSource.Object,
            _mockTarget.Object);

        // Act
        await deploymentService.Deploy(CancellationToken.None);

        // Assert
        _mockTarget.Verify(t => t.DeployScript(
                It.IsAny<Script>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _mockLogger.Verify(log =>
                log.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((value, type) => string.Equals(
                        "Script Script1 is already deployed",
                        value.ToString(),
                        StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ScriptCanRepeatWithHashChange_DeploysAgain()
    {
        // Arrange
        var deployedScripts = new List<ScriptDeployed>
        {
            new("Script1") { ContentsHash = "old_hash" }
        };

        var scriptsToDeploy = new Dictionary<string, Script>
        {
            {
                "Script1",
                new Script("Script1", "TestContent1")
                {
                    Name = "Script1", ContentsHash = "new_hash", CanRepeat = true
                }
            }
        };

        _mockTarget
            .Setup(t => t.GetDeployedScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployedScripts);

        _mockSource
            .Setup(s => s.GetScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptsToDeploy);

        var deploymentService = new DeploymentService(
            _mockLogger.Object,
            _mockSource.Object,
            _mockTarget.Object);

        // Act
        await deploymentService.Deploy(CancellationToken.None);

        // Assert
        _mockTarget.Verify(t => t.DeployScript(
                It.IsAny<Script>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockLogger.Verify(log =>
                log.Log(LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((value, type) => string.Equals(
                        "Script Script1 is already deployed, but can be repeated",
                        value.ToString(),
                        StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ScriptWithDependencyNotDeployed_DoesNotDeployScript()
    {
        // Arrange
        var deployedScripts = new List<ScriptDeployed>();

        var scriptsToDeploy = new Dictionary<string, Script>
        {
            {
                "Script2",
                new Script("Script2", "TestContent2")
                {
                    Name = "Script2", DependsOn = "Script1"
                }
            }
        };

        _mockTarget
            .Setup(t => t.GetDeployedScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployedScripts);

        _mockSource
            .Setup(s => s.GetScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptsToDeploy);

        var deploymentService = new DeploymentService(
            _mockLogger.Object,
            _mockSource.Object,
            _mockTarget.Object);

        // Act
        await deploymentService.Deploy(CancellationToken.None);

        // Assert
        _mockTarget.Verify(t => t.DeployScript(It.IsAny<Script>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockLogger.Verify(log =>
                log.Log(LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((value, type) => string.Equals(
                        "Dependency Script1 is not deployed",
                        value.ToString(),
                        StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
