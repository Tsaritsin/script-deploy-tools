using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

/// <summary>
/// Provides services for deploying scripts, managing dependencies, and logging deployment progress.
/// Represents the implementation of the deployment service responsible for handling script deployment processes.
/// </summary>
public class DeploymentService(
    ILogger logger,
    IDeploySource scriptSource,
    IDeployTarget target,
    DeploymentOptions options) : IDeploymentService
{
    #region Implementation IDeploymentService

    /// <summary>
    /// Initiates the deployment process, prepares the deployment target, determines the scripts to deploy,
    /// and handles script deployments while logging relevant information.
    /// </summary>
    /// <param name="scripts">Scripts to deployed</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
    public virtual async Task<DeploymentResult> Deploy(IReadOnlyCollection<IScript> scripts,
                                                       CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Deployment started");

            var sortedScripts = SortScriptsHelper.Sort(scripts);

            var deployScriptStatuses = new Dictionary<string, DeployScriptStatuses>();

            foreach (var script in sortedScripts)
            {
                var deployStatus = await DeployScript(script, cancellationToken);

                deployScriptStatuses.Add(script.ScriptKey, deployStatus);

                var scriptError = GetErrorByDeployStatus(deployStatus);

                if (string.IsNullOrWhiteSpace(scriptError))
                    continue;

                return DeploymentResult.Error(
                    deployScriptStatuses,
                    $"Script {script.ScriptKey} failed: {scriptError}.");
            }

            logger.LogDebug("Deployment completed");

            return DeploymentResult.Success(deployScriptStatuses);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Deployment failed");

            return DeploymentResult.Error(ex.Message);
        }
    }

    #endregion

    private string GetErrorByDeployStatus(DeployScriptStatuses status)
    {
        return status switch
        {
            DeployScriptStatuses.DependencyMissing => "Dependency missing",
            DeployScriptStatuses.WrongContent => "Wrong content",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Determines if a script is actual based on its "ActualBefore" property and the deployed scripts in the target.
    /// </summary>
    /// <param name="script">The script to evaluate for actuality.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
    /// <returns>True if the script is actual; otherwise false.</returns>
    protected virtual async Task<bool> IsActual(IScript script, CancellationToken cancellationToken)
    {
        if (script.ActualBefore is null)
            return true;

        var deployedInfo = await target.GetDeployedInfo(script.ActualBefore, cancellationToken);

        if (deployedInfo is null)
            return true;

        logger.LogWarning("Script {DependencyScript} is not actual after {ActualBefore}",
            script.ScriptKey,
            script.ActualBefore);

        return false;
    }

    /// <summary>
    /// Determines whether the dependency of a given script has already been deployed.
    /// </summary>
    /// <param name="script">The script whose dependency needs to be checked.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
    /// <returns>True if the dependency is deployed; otherwise, false.</returns>
    protected virtual async Task<bool> DependencyIsDeployed(IScript script, CancellationToken cancellationToken)
    {
        if (script.DependsOn is null)
            return true;

        var deployedInfo = await target.GetDeployedInfo(script.DependsOn, cancellationToken);

        if (deployedInfo is not null)
            return true;

        logger.LogError("Dependency {DependencyScript} is not deployed before {Script}",
            script.DependsOn,
            script.ScriptKey);

        return false;
    }

    /// <summary>
    ///     Returns the SHA256 hash of the supplied content
    /// </summary>
    /// <returns>The hash.</returns>
    /// <param name="content">Content.</param>
    protected virtual string GenerateHash(string content)
    {
        using var algorithm = SHA256.Create();

        return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    /// <summary>
    /// Determines if a script already deployed or can be redeployed based on its settings and changes in content.
    /// </summary>
    /// <param name="script">The script under evaluation for redeployment.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the script can be redeployed; otherwise false.</returns>
    protected virtual async Task<bool> IsDeployed(IScript script, CancellationToken cancellationToken)
    {
        var deployedInfo = await target.GetDeployedInfo(script.ScriptKey, cancellationToken);

        if (deployedInfo is null)
            return false;

        if (!script.CanRepeat)
        {
            logger.LogDebug("Script {ScriptKey} is already deployed", script.ScriptKey);
            return true;
        }

        var hashNotChanged = string.Equals(
            script.ContentsHash,
            deployedInfo.ContentsHash,
            StringComparison.OrdinalIgnoreCase);

        if (hashNotChanged)
        {
            logger.LogDebug("Script {ScriptKey} is not changed", script.ScriptKey);
        }

        return hashNotChanged;
    }

    /// <summary>
    /// Sets the content of the specified script by retrieving it from the deployment source,
    /// validates the content for emptiness, and updates the content's hash if applicable.
    /// Logs an error if the script content is empty.
    /// </summary>
    /// <param name="script">The script whose content needs to be set and validated.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete, used to cancel the operation if needed.</param>
    /// <returns>True if the script content was successfully set and is valid; otherwise, false.</returns>
    protected virtual async Task<bool> SetScriptContent(IScript script, CancellationToken cancellationToken)
    {
        script.Content = await scriptSource.GetScriptContent(script.Source, cancellationToken);

        var isEmpty = string.IsNullOrWhiteSpace(script.Content);

        if (isEmpty)
        {
            logger.LogError("Script {ScriptKey} is empty", script.ScriptKey);

            return false;
        }

        if (script.CanRepeat)
        {
            script.ContentsHash = GenerateHash(script.Content!);
        }

        return true;
    }

    /// <summary>
    /// Deploys a single script, ensuring its dependencies are already deployed, and logs the deployment status.
    /// </summary>
    /// <param name="script">The script to deploy.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
    protected virtual async Task<DeployScriptStatuses> DeployScript(IScript script, CancellationToken cancellationToken)
    {
        var isActual = await IsActual(script, cancellationToken);

        if (!isActual)
            return DeployScriptStatuses.NotActual;

        var contentIsValid = await SetScriptContent(script, cancellationToken);

        if (!contentIsValid)
            return DeployScriptStatuses.WrongContent;

        var isDeployed = await IsDeployed(script, cancellationToken);

        if (isDeployed)
            return DeployScriptStatuses.AlreadyDeployed;

        var dependencyIsDeployed = await DependencyIsDeployed(script, cancellationToken);

        if (!dependencyIsDeployed)
            return DeployScriptStatuses.DependencyMissing;

        await target.DeployScript(script, cancellationToken);

        await RegisterMigrations(script, cancellationToken);

        logger.LogInformation("Script {ScriptKey} deployed", script.ScriptKey);

        return DeployScriptStatuses.Deployed;
    }

    /// <summary>
    /// Registers migrations for the given script if registration is enabled and initialization is not skipped.
    /// Updates migration info using a custom migration script if provided in the options.
    /// </summary>
    /// <param name="script">The script for which migrations are to be registered.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
    protected virtual async ValueTask RegisterMigrations(IScript script, CancellationToken cancellationToken)
    {
        var canDoRegistration = !script.IsInitializeTarget &&
                                !options.DisableRegistrationOfMigrations;

        if (!canDoRegistration)
            return;

        if (options.InsertMigrationScript is null)
        {
            logger.LogWarning("InsertMigrationScript is not set, registration of migrations will be skipped");
            options.DisableRegistrationOfMigrations = true;
            return;
        }

        var insertMigrationScript = options.InsertMigrationScript;

        insertMigrationScript.ScriptParameters[nameof(IDeployedInfo.ScriptKey)] = script.ScriptKey;
        insertMigrationScript.ScriptParameters[nameof(IDeployedInfo.ContentsHash)] = script.ContentsHash;

        await target.DeployScript(insertMigrationScript, cancellationToken);
    }
}
