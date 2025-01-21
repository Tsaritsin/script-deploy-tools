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
    IConfiguration configuration)
{
    private readonly ILogger _logger = Log.ForContext<DeployHelper>();

    public async Task Deploy(CancellationToken cancellationToken)
    {
        try
        {
            _logger.Information("Deploying...");

            var deploySettings = GetDeploySettings();

            var connectionString = ConnectionStringHelper.GetConnectionStringBySettings(deploySettings);

            var deployService = new DeployBuilder()
                .AddLogger(loggerFactory.CreateLogger<IDeploymentService>())
                .FromEmbeddedResources(options =>
                {
                    options.Assemblies = [typeof(DeployHelper).Assembly];
                    options.ScriptExtension = ".sql";
                })
                .ToSqlServer(options =>
                {
                    options.ConnectionString = connectionString;
                    options.DatabaseCreationScript = "INITIALIZE_DATABASE";
                    options.DatabaseParametersScript = "SET_DATABASE_PARAMETERS";
                    options.DatabaseName = deploySettings.DatabaseName;
                    options.DefaultFilePrefix = deploySettings.DefaultFilePrefix;
                    options.DataPath = deploySettings.DataPath;
                })
                .Build();

            await deployService.Deploy(cancellationToken);

            _logger.Information("Deployment completed");
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
            .GetSection(Constants.Infrastructure.ConnectionStrings.DeploySettings)
            .Bind(deploySettings);

        return deploySettings;
    }
}
