using Microsoft.Extensions.Logging;
using Moq;

namespace ScriptDeployTools.Tests;

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

    public void Dispose()
    {
    }
}

public class DeploymentServiceTests : IClassFixture<DeploymentServiceFixture>
{
    private readonly DeploymentServiceFixture _fixture;

    public DeploymentServiceTests(DeploymentServiceFixture fixture)
    {
        _fixture = fixture;

        ResetFixture();
    }

    private void ResetFixture()
    {
        _fixture.MockLogger.Reset();
        _fixture.MockSource.Reset();
        _fixture.MockTarget.Reset();
    }

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
            { "Script1", new Script("Script1", "TestContent1") { Name = "Script1", ContentsHash = newHash, CanRepeat = canRepeat } }
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
