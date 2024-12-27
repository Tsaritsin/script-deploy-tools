namespace ScriptDeployTools;

public class ScriptDeployed(
    string name)
{
    /// <summary>
    /// Scrip name from source
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Hash of content
    /// </summary>
    public string? ContentsHash { get; set; }
}
