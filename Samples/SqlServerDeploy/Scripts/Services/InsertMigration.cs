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
}
