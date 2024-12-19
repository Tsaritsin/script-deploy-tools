namespace ScriptDeployTools.Targets.SqlServer;

public record SqlServerTargetOptions
{
    public string? ConnectionString { get; set; }
    public string? DatabaseCreationScript { get; set; }
    public string? DataPath { get; set; }
    public string? DefaultFilePrefix { get; set; }
    public string? DatabaseName { get; set; }
    public string VersionTableSchema { get; set; } = "dbo";
    public string VersionTableName { get; set; } = "Migrations";
}
