namespace SqlServerDeploy.Services.ConnectionString;

internal record ConnectionStringSettings
{
    public string? DataSource { get; set; }
    public string? InitialCatalog { get; set; }
    public bool IntegratedSecurity { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
}
