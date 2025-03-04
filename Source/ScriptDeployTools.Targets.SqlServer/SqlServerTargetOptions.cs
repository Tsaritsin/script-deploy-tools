namespace ScriptDeployTools.Targets.SqlServer;

public record SqlServerTargetOptions
{
    public string? ConnectionString { get; set; }
    public IScript? GetDeployedInfoScript { get; set; }
}
