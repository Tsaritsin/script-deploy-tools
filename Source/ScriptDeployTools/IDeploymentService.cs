namespace ScriptDeployTools;

/// <summary>
/// Defines a deployment service responsible for managing the deployment process.
/// </summary>
public interface IDeploymentService
{
    /// <summary>
    /// Initiates the deployment process while observing a provided cancellation token.
    /// </summary>
    /// <param name="scripts">Scripts to be deployed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<DeploymentResult> Deploy(IReadOnlyCollection<IScript> scripts, CancellationToken cancellationToken);
}
