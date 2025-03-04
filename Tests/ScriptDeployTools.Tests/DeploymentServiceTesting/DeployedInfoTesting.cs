namespace ScriptDeployTools.Tests.DeploymentServiceTesting;

internal record DeployedInfoTesting(
    string ScriptKey) : IDeployedInfo
{
    public string? ContentsHash { get; set; }
}
