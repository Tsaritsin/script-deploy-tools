namespace SqlServerDeploy.Services;

internal record DeploySettings
{
    public string? DataSource { get; set; }
    public bool IntegratedSecurity { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? DefaultFilePrefix { get; set; }
    public string? DataPath { get; set; }
    public string? DatabaseName { get; set; }
}
