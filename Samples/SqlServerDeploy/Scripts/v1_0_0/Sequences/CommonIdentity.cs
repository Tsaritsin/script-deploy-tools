using SqlServerDeploy.Constants;
using SqlServerDeploy.Services;

namespace SqlServerDeploy.Scripts.v1_0_0.Sequences;

/// <summary>
///     Create sequence of identifiers IDENTITY_Common
/// </summary>
internal record CommonIdentity() : ScriptBase("IDENTITY_COMMON")
{
    public override string DependsOn => ScriptNames.InitializeVersion;

    public override string Source => "SqlServerDeploy.Scripts.v1_0_0.Sequences.IDENTITY_Common.sql";
}
