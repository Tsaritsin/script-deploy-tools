namespace ScriptDeployTools;

public class Script(
    string key,
    string content)
{
    /// <summary>
    /// Gets the name of the script.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Gets the content of the script.
    /// </summary>
    /// <value></value>
    public string Content { get; } = content;

    public ScriptManifest? Manifest { get; set; }
}
