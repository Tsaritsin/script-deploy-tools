using Microsoft.Extensions.Logging;
using Moq;

namespace ScriptDeployTools.Tests;

/// <summary>
/// Provides a fixture for setting up and managing dependencies for testing the DeploymentService.
/// Include mocks for ILogger, IDeploySource, and IDeployTarget, and an instance of DeploymentService.
/// </summary>
public class DeploymentServiceFixture : IDisposable
{
    public Mock<ILogger> MockLogger { get; }
    public Mock<IDeploySource> MockSource { get; }
    public Mock<IDeployTarget> MockTarget { get; }
    public IDeploymentService DeploymentService { get; }

    public DeploymentServiceFixture()
    {
        MockLogger = new Mock<ILogger>();
        MockSource = new Mock<IDeploySource>();
        MockTarget = new Mock<IDeployTarget>();

        DeploymentService = new DeploymentService(
            MockLogger.Object,
            MockSource.Object,
            MockTarget.Object);
    }

    /// <summary>
    /// Cleans up resources held by the DeploymentServiceFixture.
    /// </summary>
    public void Dispose()
    {
    }
}

/// <summary>
/// Contains unit tests for the DeploymentService, covering various deployment scenarios
/// such as scripts already deployed, script dependencies, and repeat deployments.
/// </summary>
public class DeploymentServiceTests : IClassFixture<DeploymentServiceFixture>
{
    private readonly DeploymentServiceFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the DeploymentServiceTests class with the specified fixture.
    /// Resets the mocks of the fixture to ensure a clean state for every test.
    /// </summary>
    public DeploymentServiceTests(DeploymentServiceFixture fixture)
    {
        _fixture = fixture;

        ResetFixture();
    }

    /// <summary>
    /// Resets the mocks in the fixture to their initial state.
    /// </summary>
    private void ResetFixture()
    {
        _fixture.MockLogger.Reset();
        _fixture.MockSource.Reset();
        _fixture.MockTarget.Reset();
    }

    /// <summary>
    /// Validates that if there are no scripts to deploy, the system logs the appropriate informational message.
    /// </summary>
    [Fact]
    public async Task NoScriptsToDeploy_LogsInformation()
    {
        // Arrange
        _fixture.MockTarget
            .Setup(t => t.GetDeployedScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScriptDeployed>());

        _fixture.MockSource
            .Setup(s => s.GetScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Script>());

        // Act
        await _fixture.DeploymentService.Deploy(CancellationToken.None);

        // Assert
        _fixture.MockLogger.Verify(log =>
                log.Log(LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((value, type) => string.Equals(
                        "No scripts to deploy",
                        value.ToString(),
                        StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Ensures that already deployed scripts are not deployed again and logs the appropriate message.
    /// </summary>
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
            { "Script1", new Script("Script1", "TestContent1") { ContentsHash = "hash1", CanRepeat = false } }
        };

        _fixture.MockTarget
            .Setup(t => t.GetDeployedScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployedScripts);

        _fixture.MockSource
            .Setup(s => s.GetScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptsToDeploy);

        // Act
        await _fixture.DeploymentService.Deploy(CancellationToken.None);

        // Assert
        _fixture.MockTarget.Verify(t => t.DeployScript(
                It.IsAny<Script>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _fixture.MockLogger.Verify(log =>
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

    /// <summary>
    /// Verifies that a script marked as repeatable with a hash change is deployed again
    /// and logs a debug message.
    /// </summary>
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
                    ContentsHash = "new_hash", CanRepeat = true
                }
            }
        };

        _fixture.MockTarget
            .Setup(t => t.GetDeployedScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployedScripts);

        _fixture.MockSource
            .Setup(s => s.GetScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptsToDeploy);

        // Act
        await _fixture.DeploymentService.Deploy(CancellationToken.None);

        // Assert
        _fixture.MockTarget.Verify(t => t.DeployScript(
                It.IsAny<Script>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _fixture.MockLogger.Verify(log =>
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

    /// <summary>
    /// Ensures that scripts with unmet dependencies are not deployed and logs an error message.
    /// </summary>
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
                    DependsOn = "Script1"
                }
            }
        };

        _fixture.MockTarget
            .Setup(t => t.GetDeployedScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployedScripts);

        _fixture.MockSource
            .Setup(s => s.GetScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptsToDeploy);

        // Act
        await _fixture.DeploymentService.Deploy(CancellationToken.None);

        // Assert
        _fixture.MockTarget.Verify(t => t.DeployScript(It.IsAny<Script>(), It.IsAny<CancellationToken>()), Times.Never);
        _fixture.MockLogger.Verify(log =>
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

    /// <summary>
    /// Tests the deployment behavior based on the script's hash value and the CanRepeat flag.
    /// Uses different combinations of hash and CanRepeat values to validate expected outcomes.
    /// </summary>
    [Theory]
    [InlineData("hash1", "hash1", false, false)] // Same hash, CanRepeat false -> no deploy
    [InlineData("hash1", "hash2", true, true)] // Changed hash, CanRepeat true -> deploy
    [InlineData("hash1", "hash2", false, false)] // Changed hash, CanRepeat false -> no deploy
    [InlineData(null, null, true, true)] // Null hash, CanRepeat true -> deploy
    public async Task ScriptDeployment_BasedOnHashAndCanRepeat(
        string? deployedHash, string? newHash, bool canRepeat, bool shouldDeploy)
    {
        // Arrange
        var deployedScripts = new List<ScriptDeployed>
        {
            new("Script1") { ContentsHash = deployedHash }
        };

        var scriptsToDeploy = new Dictionary<string, Script>
        {
            { "Script1", new Script("Script1", "TestContent1") { ContentsHash = newHash, CanRepeat = canRepeat } }
        };

        _fixture.MockTarget
            .Setup(t => t.GetDeployedScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployedScripts);

        _fixture.MockSource
            .Setup(s => s.GetScripts(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptsToDeploy);

        // Act
        await _fixture.DeploymentService.Deploy(CancellationToken.None);

        // Assert
        _fixture.MockTarget.Verify(t => t.DeployScript(
            It.IsAny<Script>(), It.IsAny<CancellationToken>()), shouldDeploy ? Times.Once() : Times.Never());
    }
}
