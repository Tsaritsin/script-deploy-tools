using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ScriptDeployTools.Sources.Embedded;

/// <summary>
/// Represents a source of scripts embedded in assemblies, providing methods to retrieve and process them.
/// </summary>
internal class EmbeddedSource(
    ILogger logger,
    EmbeddedSourceOptions options) : IDeploySource

{
    #region Fields

    /// <summary>
    /// A dictionary containing loaded scripts, keyed by their unique identifiers.
    /// </summary>
    private readonly Dictionary<string, Script> _scripts = new();

    #endregion

    #region Methods

    /// <summary>
    /// Retrieves a script by its name.
    /// </summary>
    /// <param name="scriptName">The name of the script to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the requested script.</returns>
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

    /// <summary>
    /// Retrieves all loaded scripts.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of scripts.</returns>
    public Task<IReadOnlyCollection<Script>> GetScripts()
    {
        LoadScripts();

        return Task.FromResult<IReadOnlyCollection<Script>>(_scripts.Values.ToArray());
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

    /// <summary>
    /// Retrieves and filters the resource names of the provided assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to extract resources from.</param>
    /// <returns>A collection of tuples containing assemblies and their filtered resource names.</returns>
    private IReadOnlyCollection<(Assembly Assembly, string[] ResourceNames)> GetResources(
        IReadOnlyCollection<Assembly> assemblies)
    {
        var resources = assemblies
            .Select(assembly => (
                Assembly: assembly,
                ResourceNames: assembly
                    .GetManifestResourceNames()
                    .Where(options.Filter)
                    .ToArray()))
            .ToArray();

        logger.LogDebug("Loaded resources from {count} assemblies", resources.Length);

        return resources;
    }

    /// <summary>
    /// Extracts the content of an embedded resource from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the resource.</param>
    /// <param name="resourceName">The name of the resource to extract.</param>
    /// <returns>A tuple containing the resource's key, extension, and content.</returns>
    private (string Key, string Extension, string Content) GetResourceContent(
        Assembly assembly,
        string resourceName)
    {
        var scriptName = Path.GetFileNameWithoutExtension(resourceName);

        var key = GetKey(scriptName);

        var scriptExtension = Path.GetExtension(resourceName).ToLowerInvariant();

        using var stream = assembly.GetManifestResourceStream(resourceName);

        ArgumentNullException.ThrowIfNull(stream);

        using var resourceStreamReader = new StreamReader(stream, options.Encoding, true);

        var resourceContent = resourceStreamReader.ReadToEnd();

        return (
            Key: key,
            Extension: scriptExtension,
            Content: resourceContent);
    }

    /// <summary>
    /// Deserializes the manifest content into a <see cref="ScriptManifest"/> object.
    /// </summary>
    /// <param name="content">The content of the manifest.</param>
    /// <param name="key">The key associated with the manifest.</param>
    /// <returns>The deserialized <see cref="ScriptManifest"/> object, or null if deserialization fails.</returns>
    private ScriptManifest? DeserializeManifest(string content, string key)
    {
        try
        {
            return JsonSerializer.Deserialize<ScriptManifest>(content);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize manifest for script {ScriptKey}", key);
            return null;
        }
    }

    /// <summary>
    /// Creates a script from its key and a dictionary of resource contents.
    /// </summary>
    /// <param name="key">The key identifying the script.</param>
    /// <param name="resourcesContent">A dictionary containing the script and manifest contents.</param>
    /// <returns>The created script, or null if creation fails.</returns>
    private Script? CreateScript(string key, Dictionary<string, string> resourcesContent)
    {
        if (!resourcesContent.TryGetValue(options.ScriptExtension!, out var scriptContent))
        {
            logger.LogError("Script {ScriptKey} not found", key);
            return null;
        }

        if (!resourcesContent.TryGetValue(options.ManifestExtension, out var manifestContent))
        {
            logger.LogError("Script {ScriptKey} has no manifest", key);
            return null;
        }

        var manifest = DeserializeManifest(manifestContent, key);

        if (manifest != null)
        {
            return new Script(key, scriptContent)
            {
                Manifest = manifest
            };
        }

        return null;
    }

    /// <summary>
    /// Extracts and organizes script and manifest contents from an assembly's resources.
    /// </summary>
    /// <param name="assemblyResources">A tuple containing an assembly and its resource names.</param>
    /// <returns>A dictionary mapping script keys to their associated resource contents.</returns>
    private Dictionary<string, Dictionary<string, string>> GetAssemblyScriptsContents(
        (Assembly Assembly, string[] ResourceNames) assemblyResources)
    {
        var scriptAndManifestContents = new Dictionary<string, Dictionary<string, string>>();

        foreach (var resourceName in assemblyResources.ResourceNames)
        {
            var resourceContent = GetResourceContent(assemblyResources.Assembly, resourceName);

            if (scriptAndManifestContents.TryGetValue(resourceContent.Key, out var contents))
            {
                contents[resourceContent.Extension] = resourceContent.Content;
            }
            else
            {
                scriptAndManifestContents[resourceContent.Key] = new Dictionary<string, string>
                {
                    { resourceContent.Extension, resourceContent.Content }
                };
            }
        }

        return scriptAndManifestContents;
    }

    /// <summary>
    /// Parses the resources of an assembly, creating scripts from the available resources.
    /// </summary>
    /// <param name="assemblyResources">A tuple containing an assembly and its resource names.</param>
    private void ParseAssemblyResources((Assembly Assembly, string[] ResourceNames) assemblyResources)
    {
        logger.LogDebug(
            "Found {count} resource(s) in {assembly}",
            assemblyResources.ResourceNames.Length,
            assemblyResources.Assembly.FullName);

        var scriptAndManifestContents = GetAssemblyScriptsContents(assemblyResources);

        foreach (var scriptAndManifest in scriptAndManifestContents)
        {
            var script = CreateScript(scriptAndManifest.Key, scriptAndManifest.Value);

            if (script is not null)
            {
                _scripts.Add(scriptAndManifest.Key, script);
            }
        }
    }

    /// <summary>
    /// Loads scripts from the configured assemblies if they are not already loaded.
    /// </summary>
    private void LoadScripts()
    {
        if (_scripts.Count != 0)
        {
            return;
        }

        var resources = GetResources(options.Assemblies!);

        foreach (var resource in resources)
        {
            ParseAssemblyResources(resource);
        }
    }

    #endregion
}
