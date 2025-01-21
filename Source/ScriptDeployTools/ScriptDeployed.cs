namespace ScriptDeployTools;

/// <summary>
/// Represents a script that has been deployed, with metadata about its name and contents.
/// </summary>
public class ScriptDeployed(
    string name)
{
    /// <summary>
    /// Script name from source.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Hash of script content, used to verify integrity.
    /// </summary>
    public string? ContentsHash { get; set; }
}
