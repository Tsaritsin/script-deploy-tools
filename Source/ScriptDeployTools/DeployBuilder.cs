using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

public class DeployBuilder : IDeployBuilder
{
    public ILogger? Logger { get; set; }

    public IDeploySource? Source { get; set; }

    public IDeployTarget? Target { get; set; }

    public IDeploymentService Build()
    {
        return new DeploymentService(
            Logger ?? throw new InvalidOperationException("Logger must be set"),
            Source ?? throw new InvalidOperationException("Source must be set"),
            Target ?? throw new InvalidOperationException("Target must be set"));
    }
}
