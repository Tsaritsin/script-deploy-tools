namespace ScriptDeployTools;

public interface IDeploySource
{
    /// <summary>
    /// Return script from source by key
    /// </summary>
    /// <param name="scriptKey"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Script?> GetScript(string scriptKey, CancellationToken cancellationToken);

    /// <summary>
    /// Return all scripts with keys
    /// </summary>
    /// <returns></returns>
    Task<IDictionary<string, Script>> GetScripts(CancellationToken cancellationToken);
}
