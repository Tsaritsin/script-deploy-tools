using Moq;
using ScriptDeployTools.Tests.TestingModels;

namespace ScriptDeployTools.Tests.DeploymentServiceTesting;

/// <summary>
/// Contains unit tests for the DeploymentService, covering various deployment scenarios
/// such as scripts already deployed, script dependencies, and repeat deployments.
/// </summary>
public class DeploymentServiceTests(
    ITestOutputHelper output)
{
    private readonly DeploymentServiceFixture _fixture = new(output);

    /// <summary>
    /// Validates that if there are no scripts to deploy, the system logs the appropriate informational message.
    /// </summary>
    [Fact]
    public async Task NoScriptsToDeploy_DoNotTryTargetDeploy()
    {
        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        Assert.True(result.DeployScriptStatuses.Count == 0, "No scripts should be deployed");

        _fixture.MockTarget.Verify(t => t.DeployScript(
                It.IsAny<IScript>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures that when the option to disable the registration of migrations is enabled, migration scripts are not
    /// deployed to the target.
    /// </summary>
    [Fact]
    public async Task DisableRegistrationOfMigrations_IDoNotDeployMigrationScripts()
    {
        // Arrange
        var script1 = _fixture.AddScriptToTesting("Script1",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = false;
                metadata.ExpectedStatus = DeployScriptStatuses.Deployed;
            });

        _fixture.Options.DisableRegistrationOfMigrations = true;

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        _fixture.Verify(script1, result);
        _fixture.Verify(_fixture.DefaultMigrationScript, Times.Never());
    }

    /// <summary>
    /// Ensures that when a migration script is specified in the options, it is correctly passed to
    /// the deployment target for execution.
    /// </summary>
    /// <returns>
    /// True if the deployment succeeds and the migration script is properly handled;
    /// otherwise, an assertion failure indicates an issue.
    /// </returns>
    [Fact]
    public async Task InsertMigrationScript_UseMigrationScriptFromOptions()
    {
        // Arrange
        var script1 = _fixture.AddScriptToTesting("Script1",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = false;
                metadata.ExpectedStatus = DeployScriptStatuses.Deployed;
            });

        var migrationScript = _fixture.AddScriptToTesting("MigrationScript1",
            script => { script.IsService = true; },
            metadata => { metadata.IsDeployed = false; });

        _fixture.Options.InsertMigrationScript = migrationScript;

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        _fixture.Verify(script1, result);
        _fixture.Verify(_fixture.DefaultMigrationScript, Times.Never());
        _fixture.Verify(migrationScript, Times.Once());
    }

    [Fact]
    public async Task InsertMigrationScriptIsNull_IDoNotDeployMigrationScripts()
    {
        // Arrange
        var script1 = _fixture.AddScriptToTesting("Script1",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = false;
                metadata.ExpectedStatus = DeployScriptStatuses.Deployed;
            });

        _fixture.Options.InsertMigrationScript = null;

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        _fixture.Verify(script1, result);
        _fixture.Verify(_fixture.DefaultMigrationScript, Times.Never());
    }

    /// <summary>
    /// Ensures that when the content of a script is empty during deployment,
    /// an appropriate error message is included in the deployment result and the deployment is marked as unsuccessful.
    /// </summary>
    [Fact]
    public async Task ScriptContentEmpty_DeploymentResultHasErrorMessage()
    {
        // Arrange
        var script1 = _fixture.AddScriptToTesting("Script1",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = false;
                metadata.ContentState = ContentStates.Empty;
                metadata.ExpectedStatus = DeployScriptStatuses.WrongContent;
            });

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: false);

        _fixture.Verify(script1, result);
    }

    /// <summary>
    /// Ensures that the deployment process does not attempt to redeploy a script that has already been
    /// marked as deployed.
    /// </summary>
    [Fact]
    public async Task ScriptAlreadyDeployed_DoesNotDeploy()
    {
        // Arrange
        var script2 = _fixture.AddScriptToTesting("Script2",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = false;
                metadata.ExpectedStatus = DeployScriptStatuses.Deployed;
            });

        var script1 = _fixture.AddScriptToTesting("Script1",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = true;
                metadata.ExpectedStatus = DeployScriptStatuses.AlreadyDeployed;
            });

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        _fixture.Verify(script1, result);
        _fixture.Verify(script2, result);
    }

    /// <summary>
    /// Verifies that a script marked as repeatable with a hash not change is not deployed again
    /// </summary>
    [Fact]
    public async Task ScriptCanRepeatWithHashNotChange_NotDeploysAgain()
    {
        // Arrange
        const string Script1Content = "script1Content_111";

        var script1 = _fixture.AddScriptToTesting("Script1",
            script =>
            {
                script.CanRepeat = true;
                script.Content = Script1Content;
            },
            metadata =>
            {
                metadata.IsDeployed = true;
                metadata.DeployedContent = Script1Content;
                metadata.ExpectedStatus = DeployScriptStatuses.AlreadyDeployed;
            });

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        _fixture.Verify(script1, result);
    }

    /// <summary>
    /// Verifies that a script marked as repeatable with a hash change is deployed again
    /// </summary>
    [Fact]
    public async Task ScriptCanRepeatWithHashChange_DeploysAgain()
    {
        // Arrange
        var script1 = _fixture.AddScriptToTesting("Script1",
            script =>
            {
                script.CanRepeat = true;
            },
            metadata =>
            {
                metadata.IsDeployed = true;
                metadata.DeployedContent = "hash1";
                metadata.ExpectedStatus = DeployScriptStatuses.Deployed;
            });

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        _fixture.Verify(script1, result);
    }

    /// <summary>
    /// Ensures that scripts with unmet dependencies are not deployed
    /// </summary>
    [Fact]
    public async Task ScriptWithDependencyNotDeployed_DoesNotDeployScript()
    {
        // Arrange
        var script1 = _fixture.AddScriptToTesting("Script1",
            script =>
            {
                script.OrderGroup = -1000;
                script.DependsOn = "Script2";
            },
            metadata => { metadata.ExpectedStatus = DeployScriptStatuses.DependencyMissing; });

        _fixture.AddScriptToTesting("Script2",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = false;
                metadata.ExpectedStatus = DeployScriptStatuses.Unknown;
            });

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: false);

        _fixture.Verify(script1, result);
    }

    [Fact]
    public async Task ScriptWithDependencyIsDeployed_DoesDeployScript()
    {
        // Arrange
        var script1 = _fixture.AddScriptToTesting("Script1",
            script =>
            {
                script.DependsOn = "Script2";
            },
            metadata => { metadata.ExpectedStatus = DeployScriptStatuses.Deployed; });

        var script2 = _fixture.AddScriptToTesting("Script2",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = true;
                metadata.ExpectedStatus = DeployScriptStatuses.AlreadyDeployed;
            });

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        _fixture.Verify(script1, result);
        _fixture.Verify(script2, result);
    }

    [Fact]
    public async Task SourceThrowException_ReturnError()
    {
        // Arrange
        _fixture.AddScriptToTesting("Script1",
            applyScript: null,
            metadata => { metadata.ContentState = ContentStates.ThrowException; });

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: false);
    }

    [Fact]
    public async Task ScriptIsActual_DoesDeployScript()
    {
        // Arrange
        var script1 = _fixture.AddScriptToTesting("Script1",
            script =>
            {
                script.OrderGroup = -1000;
                script.ActualBefore = "Script1.1";
            },
            metadata => { metadata.ExpectedStatus = DeployScriptStatuses.Deployed; });

        var script11 = _fixture.AddScriptToTesting("Script1.1",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = false;
                metadata.ExpectedStatus = DeployScriptStatuses.Deployed;
            });

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        _fixture.Verify(script1, result);
        _fixture.Verify(script11, result);
    }

    [Fact]
    public async Task ScriptIsNotActual_DoesNotDeployScript()
    {
        // Arrange
        var script1 = _fixture.AddScriptToTesting("Script1",
            script =>
            {
                script.OrderGroup = -1000;
                script.ActualBefore = "Script1.1";
            },
            metadata => { metadata.ExpectedStatus = DeployScriptStatuses.NotActual; });

        var script11 = _fixture.AddScriptToTesting("Script1.1",
            applyScript: null,
            metadata =>
            {
                metadata.IsDeployed = true;
                metadata.ExpectedStatus = DeployScriptStatuses.AlreadyDeployed;
            });

        // Act
        var result = await _fixture.DeploymentService.Deploy(_fixture.ScriptsToTesting, CancellationToken.None);

        // Assert
        _fixture.CheckIsSuccess(result, expectedIsSuccess: true);

        _fixture.Verify(script1, result);
        _fixture.Verify(script11, result);
    }
}
