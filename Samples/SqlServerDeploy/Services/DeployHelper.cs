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
    IReadOnlyCollection<IScript> scripts)
{
    private readonly ILogger _logger = Log.ForContext<DeployHelper>();

    public async Task Deploy(CancellationToken cancellationToken)
    {
        try
        {
            var deploySettings = GetDeploySettings();

            var connectionString = ConnectionStringHelper.GetConnectionStringBySettings(deploySettings);

            var deployService = new DeployBuilder()
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
                })
                .Build();

            await deployService.Deploy(scripts, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Something went wrong");
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
