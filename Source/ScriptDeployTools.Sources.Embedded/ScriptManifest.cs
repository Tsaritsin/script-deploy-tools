namespace ScriptDeployTools.Sources.Embedded;

internal class ScriptManifest
{
    public string? Name { get; set; }

    public string? DependsOn { get; set; }

    public string? Description { get; set; }

    public bool CanRepeat { get; set; }
}
