using SqlServerDeploy.Services;

namespace SqlServerDeploy.Scripts.Services;

/// <summary>
///     Create table DeviceTypes
/// </summary>
internal record GetDeployedScripts() : ScriptBase("GET_DEPLOYED_SCRIPTS")
{
    public override string? DependsOn => null;

    public override bool IsService => true;

    public override string Source => "SqlServerDeploy.Scripts.Services.GetDeployedScripts.sql";
}
