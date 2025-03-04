using SqlServerDeploy.Constants;
using SqlServerDeploy.Services;

namespace SqlServerDeploy.Scripts.v1_0_0.Tables;

/// <summary>
///     Create table Migrations
/// </summary>
internal record Migrations() : ScriptBase(ScriptNames.InitializeVersion)
{
    public override string? DependsOn => null;

    public override int OrderGroup => -1;

    public override string Source => "SqlServerDeploy.Scripts.v1_0_0.Tables.Migrations.sql";
}
