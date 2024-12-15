namespace ScriptDeployTools;

public interface IDeploySource
{
    Task<Script?> GetScript(string scriptName);

    Task<IReadOnlyCollection<Script>> GetScripts();
}
