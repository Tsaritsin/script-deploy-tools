namespace SqlServerDeploy.Services.Deployment;

internal record DeploySettings
{
    public string? DefaultFilePrefix { get; set; }
    public string? DataPath { get; set; }
    public string? DatabaseName { get; set; }
}
