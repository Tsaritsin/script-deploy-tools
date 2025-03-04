using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SqlServerDeploy.Services;

Log.Logger = LogsHelper.CreateLogger().ForContext<Program>();

try
{
    Log.Information("Starting host");

    var builder = Host.CreateApplicationBuilder(args);

    var services = builder.Services;

    services.AddSerilog();
    services.AddScripts();
    services.AddSingleton<DeployHelper>();

    using var host = builder.Build();

    await host.StartAsync();

    var deployHelper = host.Services.GetRequiredService<DeployHelper>();
    var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

    await deployHelper.Deploy(applicationLifetime.ApplicationStopping);

    await Log.CloseAndFlushAsync();

    await host.StopAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Something went wrong");

    await Log.CloseAndFlushAsync();
}
