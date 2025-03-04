namespace ScriptDeployTools;

/// <summary>
/// Script to deploy
/// </summary>
public interface IDeployedInfo
{
    /// <summary>
    /// Gets the key of the script.
    /// </summary>
    string ScriptKey { get; }

    /// <summary>
    /// Hash of content
    /// </summary>
    string? ContentsHash { get; set; }
}
