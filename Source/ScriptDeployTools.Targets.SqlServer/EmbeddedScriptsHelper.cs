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

    public Task<Script?> GetScript(string scriptName)
    {
        logger.LogDebug("Get script {scriptName}", scriptName);

        LoadScripts();

        var key = GetKey(scriptName);

        if (!_scripts.TryGetValue(key, out var script))
        {
            logger.LogError("Script {scriptName} not found", scriptName);

            return Task.FromResult<Script?>(null);
        }

        return Task.FromResult(script)!;
    }

    private void LoadScripts()
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

            var content = GetResourceContent(assembly, resourceName);

            var script = new Script(key, content)
            {
                Manifest = new ScriptManifest
                {
                    Name = scriptName
                }
            };

            _scripts.Add(key, script);
        }
    }

    private string GetResourceContent(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);

        ArgumentNullException.ThrowIfNull(stream);

        using var resourceStreamReader = new StreamReader(stream, Encoding.UTF8, true);

        var resourceContent = resourceStreamReader.ReadToEnd();

        return resourceContent;
    }

    /// <summary>
    /// Generates a unique key for a script name by converting it to lowercase.
    /// </summary>
    /// <param name="scriptName">The name of the script.</param>
    /// <returns>The normalized key for the script.</returns>
    private string GetKey(string scriptName)
    {
        return scriptName.ToLowerInvariant();
    }
}
