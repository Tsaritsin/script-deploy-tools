namespace ScriptDeployTools;

public interface IDeploySource
{
    Task<string?> GetScript(string scriptName);
}
