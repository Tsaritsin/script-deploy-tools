namespace ScriptDeployTools.Tests.TestingModels;

internal record TestMetaData
{
    public ContentStates ContentState { get; set; }
    public bool IsDeployed { get; set; }
    public string DeployedContent { get; set; } = string.Empty;
    public DeployScriptStatuses ExpectedStatus { get; set; }
}

internal enum ContentStates
{
    Default,
    Empty,
    ThrowException
}
