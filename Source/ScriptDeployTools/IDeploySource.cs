namespace ScriptDeployTools;

/// <summary>
/// Defines methods for interacting with a deployment script source.
/// </summary>
public interface IDeploySource
{
    /// <summary>
    /// Return script from source by key
    /// </summary>
    /// <param name="scriptKey">The unique key identifying the script.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A script object if found, otherwise null.</returns>
    Task<Script?> GetScript(string scriptKey, CancellationToken cancellationToken);

    /// <summary>
    /// Return all scripts with keys
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A dictionary containing script keys and their corresponding scripts.</returns>
    Task<IDictionary<string, Script>> GetScripts(CancellationToken cancellationToken);
}
