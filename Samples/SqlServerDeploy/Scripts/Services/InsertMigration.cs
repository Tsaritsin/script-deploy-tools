using ScriptDeployTools;
using SqlServerDeploy.Services;

namespace SqlServerDeploy.Scripts.Services;

/// <summary>
///     Create table DeviceTypes
/// </summary>
internal record InsertMigration() : ScriptBase("INSERT_MIGRATION")
{
    public override string? DependsOn => null;

    public override bool IsService => true;

    public override string Source => "SqlServerDeploy.Scripts.Services.InsertMigration.sql";

    public override IDictionary<string, string?> ScriptParameters => new Dictionary<string, string?>
    {
        [nameof(IDeployedInfo.ScriptKey)] = "ScriptKey",
        [nameof(IDeployedInfo.ContentsHash)] = "ContentsHash"
    };
}
