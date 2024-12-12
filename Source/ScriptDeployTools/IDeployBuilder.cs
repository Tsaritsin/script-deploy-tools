using Microsoft.Extensions.Logging;

namespace ScriptDeployTools;

public interface IDeployBuilder
{
    public ILogger? Logger { get; set; }

    public IDeploySource? Source { get; set; }

    public IDeployTarget? Target { get; set; }

    IDeploymentService Build(Func<IDeploymentService>? factory = null);
}
