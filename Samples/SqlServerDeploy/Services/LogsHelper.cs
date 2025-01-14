using Microsoft.Extensions.Configuration;
using Serilog;

namespace SqlServerDeploy.Services;

internal class LogsHelper
{
    public static ILogger CreateLogger()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(currentDirectory)
            .AddJsonFile("logsettings.json")
            .AddJsonFile($"logsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true)
            .Build();

        var enableSelfLogs = configuration.GetValue<bool>("EnableSelfLogs");

        if (enableSelfLogs)
        {
            var logsDirectory = Path.Combine(currentDirectory, "logs");

            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            var fileName = File.AppendText(Path.Combine(logsDirectory, "_SerilogSelfLogs.txt"));

            Serilog.Debugging.SelfLog.Enable(TextWriter.Synchronized(fileName));
            Serilog.Debugging.SelfLog.Enable(Console.Error);
        }

        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}
