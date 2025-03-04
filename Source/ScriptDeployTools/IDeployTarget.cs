namespace ScriptDeployTools;

/// <summary>
/// Represents a deployment target that provides methods for preparing
/// and executing script deployments as well as retrieving deployed scripts.
/// </summary>
public interface IDeployTarget
{
    /// <summary>
    /// DeployedInfo of script
    /// </summary>
    /// <param name="scriptKey">Key of script</param>
    /// <param name="cancellationToken">Token used to signal cancellation of the operation.</param>
    /// <returns>A Information of deployed script registered in the target environment.</returns>
    ValueTask<IDeployedInfo?> GetDeployedInfo(string scriptKey, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the deployment of the provided script to the target environment.
    /// </summary>
    /// <param name="script">The script to be deployed.</param>
    /// <param name="cancellationToken">Token used to signal cancellation of the operation.</param>
    Task DeployScript(IScript script, CancellationToken cancellationToken);
}
