using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

public class DeployBuilder : IDeployBuilder
{
    public ILogger? Logger { get; set; }

    public IDeploySource? Source { get; set; }

    public IDeployTarget? Target { get; set; }

    public IDeploymentService Build(Func<IDeploymentService>? factory = null)
    {
        return factory != null
            ? factory()
            : new DeploymentService(
                Logger ?? throw new InvalidOperationException("Logger must be set"),
                Target ?? throw new InvalidOperationException("Target must be set"));
    }
}
