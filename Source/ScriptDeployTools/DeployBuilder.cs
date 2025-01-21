using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

/// <summary>
/// Provides an implementation for building and configuring deployment logic.
/// </summary>
public class DeployBuilder : IDeployBuilder
{
    /// <summary>
    /// Gets or sets an <see cref="ILogger"/> instance used for logging deployment-related activities.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Gets or sets the source of deployment scripts, implementing <see cref="IDeploySource"/>.
    /// </summary>
    public IDeploySource? Source { get; set; }

    /// <summary>
    /// Gets or sets the deployment target, implementing <see cref="IDeployTarget"/>.
    /// </summary>
    public IDeployTarget? Target { get; set; }

    /// <summary>
    /// Builds an <see cref="IDeploymentService"/> instance based on the configured
    /// <see cref="Logger"/>, <see cref="Source"/>, and <see cref="Target"/>.
    /// </summary>
    /// <returns>A fully configured <see cref="IDeploymentService"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when one or more required properties are not set.</exception>
    public IDeploymentService Build()
    {
        return new DeploymentService(
            Logger ?? throw new InvalidOperationException("Logger must be set"),
            Source ?? throw new InvalidOperationException("Source must be set"),
            Target ?? throw new InvalidOperationException("Target must be set"));
    }
}
