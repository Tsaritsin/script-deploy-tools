using SqlServerDeploy.Constants;
using SqlServerDeploy.Services;

namespace SqlServerDeploy.Scripts.v1_0_0.Database;

/// <summary>
///     Script to create database
/// </summary>
internal record InitializeDatabase() : ScriptBase("INITIALIZE_DATABASE")
{
    public override string? DependsOn => null;

    public override int OrderGroup => -1000;

    public override string ActualBefore => ScriptNames.InitializeVersion;

    public override bool IsInitializeTarget => true;

    public override string Source => "SqlServerDeploy.Scripts.v1_0_0.Database.InitializeDatabase.sql";
}
