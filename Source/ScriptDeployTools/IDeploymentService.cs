namespace ScriptDeployTools;

public interface IDeploymentService
{
    Task Deploy(CancellationToken cancellationToken);
}
