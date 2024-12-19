using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

internal class DeploymentService(
    ILogger logger,
    IDeployTarget target) : IDeploymentService
{
    public async Task Deploy(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Prepare to deploy");
            await target.PrepareToDeploy(cancellationToken);
            logger.LogDebug("Prepare completed");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to deploy");
        }
    }
}
