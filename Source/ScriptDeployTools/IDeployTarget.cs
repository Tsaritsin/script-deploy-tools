namespace ScriptDeployTools;

public interface IDeployTarget
{
    Task PrepareToDeploy();
}
