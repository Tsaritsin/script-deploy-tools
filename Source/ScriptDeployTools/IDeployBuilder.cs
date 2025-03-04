using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

/// <summary>
/// Defines a builder interface for creating deployment-related components, such as source, target, and services.
/// </summary>
public interface IDeployBuilder
{
    /// <summary>
    /// Gets or sets the logger used for deployment operations.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Gets or sets the source of deployment scripts.
    /// </summary>
    public IDeploySource? Source { get; set; }

    /// <summary>
    /// Gets or sets the target for deployment operations.
    /// </summary>
    public IDeployTarget? Target { get; set; }

    /// <summary>
    /// Gets or sets the deployment options, which include configurations for
    /// deployment-related scripts and processes.
    /// </summary>
    public DeploymentOptions? Options { get; set; }

    /// <summary>
    /// Builds an <see cref="IDeploymentService"/> instance based on the current
    /// configuration of the builder.
    /// </summary>
    /// <returns>A fully configured <see cref="IDeploymentService"/> instance.</returns>
    IDeploymentService Build();
}
