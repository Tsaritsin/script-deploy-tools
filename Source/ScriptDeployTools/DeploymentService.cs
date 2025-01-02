using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

/// <summary>
/// Provides services for deploying scripts, managing dependencies, and logging deployment progress.
/// Represents the implementation of the deployment service responsible for handling script deployment processes.
/// </summary>
internal class DeploymentService(
    ILogger logger,
    IDeploySource scriptSource,
    IDeployTarget target) : IDeploymentService
{
    /// <summary>
    /// Initiates the deployment process, prepares the deployment target, determines the scripts to deploy,
    /// and handles script deployments while logging relevant information.
    /// </summary>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
    public async Task Deploy(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Prepare to deploy");

            await target.PrepareToDeploy(cancellationToken);

            logger.LogDebug("Prepare completed");

            var deployedScripts = await target.GetDeployedScripts(cancellationToken);

            var scripsToDeploy = await GetScriptsToDeploy(deployedScripts, cancellationToken);

            foreach (var script in scripsToDeploy)
            {
                await DeployScript(script, deployedScripts, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to deploy");
        }
    }

    /// <summary>
    /// Deploys a single script, ensuring its dependencies are already deployed, and logs the deployment status.
    /// </summary>
    /// <param name="script">The script to deploy.</param>
    /// <param name="deployedScripts">The list of scripts that are already deployed.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
    private async Task DeployScript(Script script,
                                    IList<ScriptDeployed> deployedScripts,
                                    CancellationToken cancellationToken)
    {
        var dependencyIsDeployed = script.DependsOn is null ||
                                   deployedScripts.Any(item => item.Name.Equals(
                                       script.DependsOn,
                                       StringComparison.OrdinalIgnoreCase));

        if (!dependencyIsDeployed)
        {
            logger.LogError("Dependency {DependencyScript} is not deployed", script.DependsOn);
            return;
        }

        logger.LogInformation($"Deploying script {script.Name}");

        await target.DeployScript(script, cancellationToken);

        deployedScripts.Add(new ScriptDeployed(script.Name!)
        {
            ContentsHash = script.ContentsHash
        });

        logger.LogInformation($"Script {script.Name} deployed");
    }

    /// <summary>
    /// Retrieves the collection of scripts that need to be deployed, excluding already deployed scripts
    /// unless they are allowed to be redeployed.
    /// </summary>
    /// <param name="deployedScripts">The list of scripts that are already deployed.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
    /// <returns>A read-only collection of scripts to deploy.</returns>
    private async Task<IReadOnlyCollection<Script>> GetScriptsToDeploy(
        IReadOnlyCollection<ScriptDeployed> deployedScripts,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Search scripts to deploying");

        var result = new List<Script>();

        var scripts = await scriptSource.GetScripts(cancellationToken);

        if (scripts.Count == 0)
        {
            logger.LogInformation("No scripts to deploy");
            return result;
        }

        var scriptsToDeploy = new Dictionary<string, Script>();

        foreach (var script in scripts)
        {
            var deployedScript = deployedScripts.FirstOrDefault(item => item.Name.Equals(
                script.Value.Name,
                StringComparison.OrdinalIgnoreCase));

            if (deployedScript != null)
            {
                var canRepeat = CanBeDeployedAgain(script.Value, deployedScript);

                if (!canRepeat)
                {
                    logger.LogInformation($"Script {script.Value.Name} is already deployed");
                    continue;
                }

                logger.LogDebug($"Script {script.Value.Name} is already deployed, but can be repeated");
            }

            scriptsToDeploy.Add(script.Key, script.Value);
        }

        if (scriptsToDeploy.Count == 0)
        {
            logger.LogInformation("No scripts to deploy");
        }

        var sortedScripts = SortScriptsByDependenciesHelper.Sort(scriptsToDeploy);

        result.AddRange(sortedScripts);

        logger.LogDebug("Found {ScriptsToDeployCount} scripts to deploying", sortedScripts.Count);

        return result;
    }

    /// <summary>
    /// Determines if a script can be redeployed based on its settings and changes in content.
    /// </summary>
    /// <param name="script">The script under evaluation for redeployment.</param>
    /// <param name="deployedScript">The corresponding deployed script.</param>
    /// <returns>True if the script can be redeployed; otherwise false.</returns>
    private static bool CanBeDeployedAgain(Script script, ScriptDeployed deployedScript)
    {
        if (!script.CanRepeat)
            return false;

        var hashNotUsed = string.IsNullOrEmpty(script.ContentsHash) &&
                          string.IsNullOrEmpty(deployedScript.ContentsHash);

        if (hashNotUsed)
            return true;

        var hashNotChanged = string.Equals(
            deployedScript.ContentsHash,
            script.ContentsHash,
            StringComparison.OrdinalIgnoreCase);

        return !hashNotChanged;
    }
}
