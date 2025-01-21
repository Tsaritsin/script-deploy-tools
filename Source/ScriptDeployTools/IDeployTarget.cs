namespace ScriptDeployTools;

/// <summary>
/// Represents a deployment target that provides methods for preparing
/// and executing script deployments as well as retrieving deployed scripts.
/// </summary>
public interface IDeployTarget
{
    /// <summary>
    /// Prepares the target environment for deployment by performing necessary setup actions.
    /// </summary>
    /// <param name="cancellationToken">Token used to signal cancellation of the operation.</param>
    Task PrepareToDeploy(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of deployed scripts from the target environment.
    /// </summary>
    /// <param name="cancellationToken">Token used to signal cancellation of the operation.</param>
    /// <returns>A list of deployed scripts registered in the target environment.</returns>
    Task<List<ScriptDeployed>> GetDeployedScripts(CancellationToken cancellationToken);

    /// <summary>
    /// Executes the deployment of the provided script to the target environment.
    /// </summary>
    /// <param name="script">The script to be deployed.</param>
    /// <param name="cancellationToken">Token used to signal cancellation of the operation.</param>
    Task DeployScript(Script script, CancellationToken cancellationToken);
}
