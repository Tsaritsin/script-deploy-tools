using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

public static class DeployBuilderExtensions
{
    public static IDeployBuilder AddLogger(this IDeployBuilder builder,
                                           ILogger logger)
    {
        builder.Logger = logger;

        return builder;
    }
}
