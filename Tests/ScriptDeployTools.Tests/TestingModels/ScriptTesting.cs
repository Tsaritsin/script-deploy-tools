namespace ScriptDeployTools.Tests.TestingModels;

internal record ScriptTesting(
    string ScriptKey) : IScript
{
    public TestMetaData TestMetaData { get; set; } = new();

    public string? DependsOn { get; set; }

    public string? Content { get; set; } = $"Content_{ScriptKey}";

    public int OrderGroup { get; set; }

    public bool IsService { get; set; }

    public string? ActualBefore { get; set; }

    public bool CanRepeat { get; set; }

    public string? ContentsHash { get; set; }

    public string Source { get; set; } = $"Source_{ScriptKey}";

    public bool IsInitializeTarget { get; set; }

    public IDictionary<string, string?> ScriptParameters => new Dictionary<string, string?>();
}
