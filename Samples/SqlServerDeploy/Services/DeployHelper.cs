using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScriptDeployTools;
using ScriptDeployTools.Sources.Embedded;
using ScriptDeployTools.Targets.SqlServer;
using Serilog;
using ILogger = Serilog.ILogger;

namespace SqlServerDeploy.Services;

internal class DeployHelper(
    ILoggerFactory loggerFactory,
    IConfiguration configuration,
    IEnumerable<IScript> scripts)
{
    private readonly ILogger _logger = Log.ForContext<DeployHelper>();

    public async Task Deploy(CancellationToken cancellationToken)
    {
        try
        {
            var deploySettings = GetDeploySettings();

            var connectionString = ConnectionStringHelper.GetConnectionStringBySettings(deploySettings);

            var deployBuilder = new DeployBuilder()
                .AddLogger(loggerFactory.CreateLogger<IDeploymentService>())
                .AddOptions(new DeploymentOptions
                {
                    InsertMigrationScript = scripts.FirstOrDefault(x =>
                        x is { IsService: true, ScriptKey: "INSERT_MIGRATION" })
                })
                .FromEmbeddedResources(options =>
                {
                    options.Assemblies = [typeof(DeployHelper).Assembly];
                })
                .ToSqlServer(options =>
                {
                    options.ConnectionString = connectionString;
                    options.GetDeployedInfoScript = scripts.FirstOrDefault(x =>
                        x is { IsService: true, ScriptKey: "GET_DEPLOYED_SCRIPTS" });
                });

            SetInitializeDatabaseParameters(deploySettings);

            await SetServiceScriptsContents(deployBuilder, cancellationToken);

            var deployService = deployBuilder.Build();

            await deployService.Deploy(scripts.ToArray(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Something went wrong");
        }
    }

    private void SetInitializeDatabaseParameters(DeploySettings deploySettings)
    {
        var initializeDatabase = scripts.FirstOrDefault(x =>
            x is { ScriptKey: "INITIALIZE_DATABASE" });

        if (initializeDatabase is not null)
        {
            initializeDatabase.ScriptParameters["DatabaseName"] = deploySettings.DatabaseName;
            initializeDatabase.ScriptParameters["DataPath"] = deploySettings.DataPath;
            initializeDatabase.ScriptParameters["DefaultFilePrefix"] = deploySettings.DefaultFilePrefix;
        }

        var setDatabaseParameters = scripts.FirstOrDefault(x =>
            x is { ScriptKey: "SET_DATABASE_PARAMETERS" });

        if (setDatabaseParameters is not null)
        {
            setDatabaseParameters.ScriptParameters["DatabaseName"] = deploySettings.DatabaseName;
        }
    }

    private async Task SetServiceScriptsContents(IDeployBuilder deployBuilder, CancellationToken cancellationToken)
    {
        var scriptSource = deployBuilder.Source ?? throw new InvalidOperationException("Source is null");

        var serviceScripts = scripts.Where(x => x.IsService);

        foreach (var serviceScript in serviceScripts)
        {
            serviceScript.Content = await scriptSource.GetScriptContent(serviceScript.Source, cancellationToken);

            if (string.IsNullOrEmpty(serviceScript.Content))
            {
                throw new InvalidOperationException($"Script content is empty: {serviceScript.ScriptKey}");
            }
        }
    }

    private DeploySettings GetDeploySettings()
    {
        var deploySettings = new DeploySettings();

        configuration
            .GetSection(Constants.Infrastructure.ConfigurationSections.DeploySettings)
            .Bind(deploySettings);

        return deploySettings;
    }
}
