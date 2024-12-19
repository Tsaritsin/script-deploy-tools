namespace ScriptDeployTools;

public class ScriptManifest
{
    public required string Name { get; set; }

    /// <summary>
    /// Key of script
    /// </summary>
    public string? DependsOn { get; set; }

    public string? Description { get; set; }

    public bool CanRepeat { get; set; }
}
