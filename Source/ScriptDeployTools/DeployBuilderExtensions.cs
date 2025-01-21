using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

/// <summary>
/// Provides extension methods for the <see cref="IDeployBuilder"/> interface.
/// </summary>
public static class DeployBuilderExtensions
{
    /// <summary>
    /// Adds a logger to the deploy builder.
    /// </summary>
    /// <param name="builder">The deploy builder to which the logger will be added.</param>
    /// <param name="logger">The logger to be added to the deploy builder.</param>
    /// <returns>The updated <see cref="IDeployBuilder"/> instance.</returns>
    public static IDeployBuilder AddLogger(this IDeployBuilder builder,
                                           ILogger logger)
    {
        builder.Logger = logger;

        return builder;
    }
}
