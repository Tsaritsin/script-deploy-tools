using Microsoft.Extensions.DependencyInjection;
using ScriptDeployTools;

namespace SqlServerDeploy.Services;

internal static class ScriptsServiceCollectionExtensions
{
    public static IServiceCollection AddScripts(this IServiceCollection collection)
    {
        var assembly = typeof(ScriptsServiceCollectionExtensions).Assembly;

        var types = assembly.DefinedTypes
            .Where(x => x is { IsClass: true, IsAbstract: false } &&
                        x.BaseType == typeof(ScriptBase))
            .ToArray();

        foreach (var scriptType in types)
            collection.AddSingleton(typeof(IScript), scriptType.AsType());

        return collection;
    }
}
