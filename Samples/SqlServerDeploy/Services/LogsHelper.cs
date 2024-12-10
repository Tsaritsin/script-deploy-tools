using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace SqlServerDeploy.Services;

internal class LogsHelper
{
    public static ILogger CreateLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Sixteen,
                applyThemeToRedirectedOutput: true)
            .CreateLogger();
    }
}
