using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

internal class DeploymentService(
    ILogger logger,
    IDeploySource scriptSource,
    IDeployTarget target) : IDeploymentService
{
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

        var scripsToDeploy = new Dictionary<string, Script>();

        foreach (var script in scripts)
        {
            var deployedScript = deployedScripts.FirstOrDefault(item => item.Name.Equals(
                script.Value.Name,
                StringComparison.OrdinalIgnoreCase));

            if (deployedScript != null)
            {
                var canRepeat = script.Value.CanRepeat &&
                                script.Value.ContentsHash is not null &&
                                deployedScript.ContentsHash is not null &&
                                !deployedScript.ContentsHash.Equals(
                                    script.Value.ContentsHash,
                                    StringComparison.OrdinalIgnoreCase);
                if (!canRepeat)
                {
                    logger.LogInformation($"Script {script.Value.Name} is already deployed");
                    continue;
                }

                logger.LogDebug($"Script {script.Value.Name} is already deployed, but can be repeated");
            }

            scripsToDeploy.Add(script.Key, script.Value);
        }

        if (scripsToDeploy.Count == 0)
        {
            logger.LogInformation("No scripts to deploy");
        }

        var sortedScripts = SortScriptsByDependenciesHelper.Sort(scripsToDeploy);

        result.AddRange(sortedScripts);

        logger.LogDebug("Found {ScripsToDeployCount} scripts to deploying", sortedScripts.Count);

        return result;
    }
}
