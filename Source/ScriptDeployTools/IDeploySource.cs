namespace ScriptDeployTools;

/// <summary>
/// Defines methods for interacting with a deployment script source.
/// </summary>
public interface IDeploySource
{
    /// <summary>
    /// Return script content from source by key
    /// </summary>
    /// <param name="scriptSource">Data is using for get content of script from source</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A script content</returns>
    Task<string?> GetScriptContent(string scriptSource, CancellationToken cancellationToken);
}
