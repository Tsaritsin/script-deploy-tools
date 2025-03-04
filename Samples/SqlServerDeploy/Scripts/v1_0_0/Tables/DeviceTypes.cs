using SqlServerDeploy.Services;

namespace SqlServerDeploy.Scripts.v1_0_0.Tables;

/// <summary>
///     Create table DeviceTypes
/// </summary>
internal record DeviceTypes() : ScriptBase("DEVICE_TYPES")
{
    public override string DependsOn => "IDENTITY_COMMON";

    public override string Source => "SqlServerDeploy.Scripts.v1_0_0.Tables.DeviceTypes.sql";
}
