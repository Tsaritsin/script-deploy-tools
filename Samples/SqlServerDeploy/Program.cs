using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SqlServerDeploy.Services;
using SqlServerDeploy.Services.Deployment;

Log.Logger = LogsHelper.CreateLogger().ForContext<Program>();

try
{
    Log.Information("Starting host");

    var builder = Host.CreateApplicationBuilder(args);

    var services = builder.Services;

    services.AddSerilog();
    services.AddSingleton<DeployHelper>();

    using var host = builder.Build();

    await host.StartAsync();

    var deployHelper = host.Services.GetRequiredService<DeployHelper>();
    var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

    var tasks = new List<Task>
    {
        host.WaitForShutdownAsync(),
        deployHelper.Deploy(applicationLifetime.ApplicationStopping)
    };

    await Task.WhenAny(tasks);

    await host.StopAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Something went wrong");
}
finally
{
    await Log.CloseAndFlushAsync();
}
