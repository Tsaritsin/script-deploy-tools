using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ScriptDeployTools.Targets.SqlServer;

internal class EmbeddedScriptsHelper(
    ILogger logger)
{
    #region Fields

    /// <summary>
    /// A dictionary containing loaded scripts, keyed by their unique identifiers.
    /// </summary>
    private readonly Dictionary<string, Script> _scripts = new();

    #endregion

    public async Task<Script?> GetScript(string scriptName, CancellationToken cancellationToken)
    {
        logger.LogDebug("Get script {scriptName}", scriptName);

        await LoadScripts(cancellationToken);

        var key = GetKey(scriptName);

        if (_scripts.TryGetValue(key, out var script))
            return script;

        logger.LogError("Script {scriptName} not found", scriptName);

        return null;
    }

    private async Task LoadScripts(CancellationToken cancellationToken)
    {
        if (_scripts.Count != 0)
        {
            return;
        }

        var assembly = GetType().Assembly;

        var resources = assembly.GetManifestResourceNames();

        foreach (var resourceName in resources)
        {
            var scriptName = Path.GetFileNameWithoutExtension(resourceName);

            var key = GetKey(scriptName);

            var content = await GetResourceContent(assembly, resourceName, cancellationToken);

            var script = new Script(key, content)
            {
                Name = scriptName
            };

            _scripts.Add(key, script);
        }
    }

    private static async Task<string> GetResourceContent(Assembly assembly,
                                                         string resourceName,
                                                         CancellationToken cancellationToken)
    {
        await using var stream = assembly.GetManifestResourceStream(resourceName);

        ArgumentNullException.ThrowIfNull(stream);

        using var resourceStreamReader = new StreamReader(stream, Encoding.UTF8, true);

        var resourceContent = await resourceStreamReader.ReadToEndAsync(cancellationToken);

        return resourceContent;
    }

    /// <summary>
    /// Generates a unique key for a script name by converting it to lowercase.
    /// </summary>
    /// <param name="scriptName">The name of the script.</param>
    /// <returns>The normalized key for the script.</returns>
    private static string GetKey(string scriptName)
    {
        return scriptName.ToLowerInvariant();
    }
}
