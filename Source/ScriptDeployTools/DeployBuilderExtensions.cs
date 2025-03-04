using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

/// <summary>
/// Provides extension methods for the <see cref="IDeployBuilder"/> interface.
/// </summary>
public static class DeployBuilderExtensions
{
    /// <summary>
    /// Adds a logger to the deployment builder.
    /// </summary>
    /// <param name="builder">The deployment builder to which the logger will be added.</param>
    /// <param name="logger">The logger to be added to the deployment builder.</param>
    /// <returns>The updated <see cref="IDeployBuilder"/> instance.</returns>
    public static IDeployBuilder AddLogger(this IDeployBuilder builder,
                                           ILogger logger)
    {
        builder.Logger = logger;

        return builder;
    }

    /// <summary>
    /// Adds deployment options to the deployment builder.
    /// </summary>
    /// <param name="builder">The deployment builder to which the options will be added.</param>
    /// <param name="options">The deployment options to be added to the deploy builder.</param>
    /// <returns>The updated <see cref="IDeployBuilder"/> instance.</returns>
    public static IDeployBuilder AddOptions(this IDeployBuilder builder,
                                            DeploymentOptions options)
    {
        builder.Options = options;

        return builder;
    }
}
