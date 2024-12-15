using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScriptDeployTools;
using ScriptDeployTools.Sources.Embedded;
using ScriptDeployTools.Targets.SqlServer;
using Serilog;
using SqlServerDeploy.Services.ConnectionString;
using ILogger = Serilog.ILogger;

namespace SqlServerDeploy.Services.Deployment;

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

            var connectionString = configuration.GetConnectionString();

            var deploySettings = new DeploySettings();

            configuration
                .GetSection(Constants.Infrastructure.ConnectionStrings.DeploySettings)
                .Bind(deploySettings);

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
                    options.DatabaseCreationScript = "InitializeDatabase";
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

        // var upgrader = DeployChanges.To.SqlDatabase(connectionString)
        //     .WithScriptsAndCodeEmbeddedInAssembly(typeof(MyAssembly).Assembly)
        //     .LogToConsole()
        //     .JournalToSqlWithHashing(scripts =>
        //
        //         scripts.WithPrefix("MyAssembly.Scripts.ByDate.")
        //             .OrderBy(s => s.Name).Concat(
        //        
        //                 scripts.WithPrefix("MyAssembly.Scripts.Dependent.")
        //                     .OrderByDependency("#requires"))
        //     )
        //     .Build()
        //     .PerformUpgrade();
    }
}
