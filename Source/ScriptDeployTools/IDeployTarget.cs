namespace ScriptDeployTools;

public interface IDeployTarget
{
    Task PrepareToDeploy(CancellationToken cancellationToken);

    Task Deploy(CancellationToken cancellationToken);
}
