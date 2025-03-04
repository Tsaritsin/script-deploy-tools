using System.Security.Cryptography;
using System.Text;
using Moq;
using ScriptDeployTools.Tests.TestingModels;

namespace ScriptDeployTools.Tests.DeploymentServiceTesting;

/// <summary>
/// Provides a fixture for setting up and managing dependencies for testing the DeploymentService.
/// Include mocks and an instance of DeploymentService.
/// </summary>
internal class DeploymentServiceFixture
{
    #region Constants

    public const string MigrationScriptKey = "MigrationScript";

    #endregion

    public DeploymentServiceFixture(ITestOutputHelper output)
    {
        DefaultMigrationScript = AddScriptToTesting(MigrationScriptKey,
            script => { script.IsService = true; },
            metadata => { metadata.IsDeployed = false; });

        Options.InsertMigrationScript = DefaultMigrationScript;

        var logger = output.BuildLoggerFor<DeploymentServiceTests>();

        DeploymentService = new DeploymentService(
            logger,
            MockSource.Object,
            MockTarget.Object,
            Options);
    }

    #region Methods

    public string GenerateHash(string content)
    {
        using var algorithm = SHA256.Create();

        return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    private void SetupScriptContent(ScriptTesting script)
    {
        var mockGetScriptContent = MockSource.Setup(s => s.GetScriptContent(
            It.Is<string>(x => x.Equals(script.Source, StringComparison.OrdinalIgnoreCase)),
            It.IsAny<CancellationToken>()));

        switch (script.TestMetaData.ContentState)
        {
            case ContentStates.Default:
                mockGetScriptContent.ReturnsAsync(script.Content);
                break;
            case ContentStates.Empty:
                mockGetScriptContent.ReturnsAsync(null as string);
                break;
            case ContentStates.ThrowException:
                mockGetScriptContent.ThrowsAsync(new Exception("Test exception"));
                break;
        }
    }

    private void SetupScriptDeployedInfo(ScriptTesting script)
    {
        var mockGetDeployedInfo = MockTarget.Setup(t => t.GetDeployedInfo(
            It.Is<string>(x => x.Equals(script.ScriptKey, StringComparison.OrdinalIgnoreCase)),
            It.IsAny<CancellationToken>()));

        if (script.TestMetaData.IsDeployed)
        {
            mockGetDeployedInfo.ReturnsAsync(new DeployedInfoTesting(script.ScriptKey)
            {
                ContentsHash = GenerateHash(script.TestMetaData.DeployedContent)
            });
        }
        else
        {
            mockGetDeployedInfo.ReturnsAsync(null as IDeployedInfo);
        }
    }

    public ScriptTesting AddScriptToTesting(string scriptKey,
                                            Action<ScriptTesting>? applyScript,
                                            Action<TestMetaData>? applyTestMetaData)
    {
        var script = new ScriptTesting(scriptKey);

        applyScript?.Invoke(script);

        applyTestMetaData?.Invoke(script.TestMetaData);

        ScriptsToTesting.Add(script);

        SetupScriptContent(script);

        SetupScriptDeployedInfo(script);

        return script;
    }

    public void CheckIsSuccess(DeploymentResult result,
                               bool expectedIsSuccess)
    {
        var expectedIsSuccessName = expectedIsSuccess ? "SUCCESS" : "FAILURE";

        Assert.True(expectedIsSuccess == result.IsSuccess, $"Deployment result should be {expectedIsSuccessName}");

        if (expectedIsSuccess == false)
        {
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage), "Deployment result should have error message");
        }
    }

    public void Verify(ScriptTesting script,
                       Times times)
    {
        MockTarget.Verify(t => t.DeployScript(
                It.Is<IScript>(x => x.Equals(script)),
                It.IsAny<CancellationToken>()),
            times);
    }

    public void Verify(ScriptTesting script,
                       DeploymentResult result)
    {
        var times = script.TestMetaData.ExpectedStatus switch
        {
            DeployScriptStatuses.Deployed => Times.Once(),
            _ => Times.Never()
        };

        MockTarget.Verify(t => t.DeployScript(
                It.Is<IScript>(x => x.Equals(script)),
                It.IsAny<CancellationToken>()),
            times);

        var serviceReturnStatus = !script.IsService;

        if (serviceReturnStatus)
        {
            Assert.True(
                script.TestMetaData.ExpectedStatus == result.DeployScriptStatuses[script.ScriptKey],
                $"Deployment result {script.ScriptKey} should be {script.TestMetaData.ExpectedStatus}");
        }
    }

    #endregion

    #region Properties

    public List<IScript> ScriptsToTesting { get; } = new();

    public ScriptTesting DefaultMigrationScript { get; }

    public Mock<IDeploySource> MockSource { get; } = new();

    public Mock<IDeployTarget> MockTarget { get; } = new();

    public DeploymentOptions Options { get; } = new();

    public IDeploymentService DeploymentService { get; }

    #endregion
}
