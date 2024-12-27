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

            logger.LogInformation("Start deploying scripts");

            var scripts = await scriptSource.GetScripts(cancellationToken);

            if (scripts.Count == 0)
            {
                logger.LogInformation("No scripts to deploy");
                return;
            }

            var deployedScripts = await target.GetDeployedScripts(cancellationToken);

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

                    logger.LogInformation($"Script {script.Value.Name} is already deployed, but can be repeated");
                }

                scripsToDeploy.Add(script.Key, script.Value);
            }

            if (scripsToDeploy.Count == 0)
            {
                logger.LogInformation("No scripts to deploy");
            }

            var sortedScripts = SortScriptsByDependenciesHelper.Sort(scripsToDeploy);

            foreach (var script in sortedScripts)
            {
                logger.LogInformation($"Deploying script {script.Name}");

                await target.DeployScript(script, cancellationToken);

                logger.LogInformation($"Script {script.Name} deployed");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to deploy");
        }
    }
}
