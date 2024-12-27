namespace ScriptDeployTools.Targets.SqlServer.Constants;

internal static class ScriptNames
{
    public const string TableExists = "ScriptDeployTools.Targets.SqlServer.Scripts.TableExists";
    public const string InitializeVersionTable = "ScriptDeployTools.Targets.SqlServer.Scripts.InitializeVersionTable";
    public const string InsertMigration = "ScriptDeployTools.Targets.SqlServer.Scripts.InsertMigration";
    public const string GetDeployedScripts = "ScriptDeployTools.Targets.SqlServer.Scripts.GetDeployedScripts";
}
