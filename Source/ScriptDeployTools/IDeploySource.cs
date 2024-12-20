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

    /// <summary>
    /// Generates a unique key for a script name by converting it to lowercase.
    /// </summary>
    /// <param name="scriptName">The name of the script.</param>
    /// <returns>The normalized key for the script.</returns>
    string GetKey(string scriptName);

    string GenerateHash(string content);
}
