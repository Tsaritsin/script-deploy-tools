using Serilog;

namespace SqlServerDeploy.Services.Deployment;

internal class DeployHelper
{
    private readonly ILogger _logger = Log.ForContext<DeployHelper>();

    public Task Deploy(CancellationToken cancellationToken)
    {
        _logger.Information("Deploying...");

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
