namespace ScriptDeployTools;

public interface IDeployTarget
{
    Task PrepareToDeploy(CancellationToken cancellationToken);

    Task<List<ScriptDeployed>> GetDeployedScripts(CancellationToken cancellationToken);

    Task DeployScript(Script script, CancellationToken cancellationToken);
}
