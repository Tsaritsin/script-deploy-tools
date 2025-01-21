namespace ScriptDeployTools.Sources.Embedded;

internal record ScriptManifest(string Key)
{
    public string? DependsOn { get; set; }

    public string? ActualBefore { get; set; }

    public string? Description { get; set; }

    public bool CanRepeat { get; set; }
}
