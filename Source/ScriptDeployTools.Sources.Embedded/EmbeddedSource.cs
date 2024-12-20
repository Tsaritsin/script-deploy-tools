using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
    /// <param name="cancellationToken"></param>
    /// <returns>A tuple containing the resource's key, extension, and content.</returns>
    private async Task<(string Key, string Extension, string Content)> GetResourceContent(
        Assembly assembly,
        string resourceName,
        CancellationToken cancellationToken)
    {
        var scriptName = Path.GetFileNameWithoutExtension(resourceName);

        var key = GetKey(scriptName);

        var scriptExtension = Path.GetExtension(resourceName).ToLowerInvariant();

        await using var stream = assembly.GetManifestResourceStream(resourceName);

        ArgumentNullException.ThrowIfNull(stream);

        using var resourceStreamReader = new StreamReader(stream, options.Encoding, true);

        var resourceContent = await resourceStreamReader.ReadToEndAsync(cancellationToken);

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

        if (manifest is null)
        {
            logger.LogError("Script {ScriptKey} has an invalid manifest", key);
            return null;
        }

        return new Script(key, scriptContent)
        {
            Name = manifest.Name,
            DependsOn = manifest.DependsOn,
            Description = manifest.Description,
            CanRepeat = manifest.CanRepeat,
            ContentsHash = GenerateHash(scriptContent)
        };
    }

    /// <summary>
    /// Extracts and organizes script and manifest contents from an assembly's resources.
    /// </summary>
    /// <param name="assemblyResources">A tuple containing an assembly and its resource names.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A dictionary mapping script keys to their associated resource contents.</returns>
    private async Task<Dictionary<string, Dictionary<string, string>>> GetAssemblyScriptsContents(
        (Assembly Assembly, string[] ResourceNames) assemblyResources,
        CancellationToken cancellationToken)
    {
        var scriptAndManifestContents = new Dictionary<string, Dictionary<string, string>>();

        foreach (var resourceName in assemblyResources.ResourceNames)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var resourceContent = await GetResourceContent(assemblyResources.Assembly, resourceName, cancellationToken);

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
        } // foreach (var resourceName in assemblyResources.ResourceNames)

        return scriptAndManifestContents;
    }

    /// <summary>
    /// Parses the resources of an assembly, creating scripts from the available resources.
    /// </summary>
    /// <param name="assemblyResources">A tuple containing an assembly and its resource names.</param>
    /// <param name="cancellationToken"></param>
    private async Task ParseAssemblyResources((Assembly Assembly, string[] ResourceNames) assemblyResources,
                                              CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Found {count} resource(s) in {assembly}",
            assemblyResources.ResourceNames.Length,
            assemblyResources.Assembly.FullName);

        var scriptAndManifestContents = await GetAssemblyScriptsContents(assemblyResources, cancellationToken);

        foreach (var scriptAndManifest in scriptAndManifestContents)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

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
    private async Task LoadScripts(CancellationToken cancellationToken)
    {
        if (_scripts.Count != 0)
        {
            return;
        }

        var resources = GetResources(options.Assemblies);

        foreach (var resource in resources)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ParseAssemblyResources(resource, cancellationToken);
        }
    }

    #endregion

    #region Implemented IDeploySource

    /// <summary>
    /// Retrieves a script by its name.
    /// </summary>
    /// <param name="scriptKey">The key of the script to retrieve.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the requested script.</returns>
    public async Task<Script?> GetScript(string scriptKey, CancellationToken cancellationToken)
    {
        logger.LogDebug("Get script {scriptKey}", scriptKey);

        await LoadScripts(cancellationToken);

        if (_scripts.TryGetValue(scriptKey, out var script))
            return script;

        logger.LogError("Script {scriptKey} not found", scriptKey);

        return null;
    }

    /// <summary>
    /// Retrieves all loaded scripts.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of scripts.</returns>
    public async Task<IDictionary<string, Script>> GetScripts(CancellationToken cancellationToken)
    {
        await LoadScripts(cancellationToken);

        return _scripts;
    }

    /// <summary>
    /// Generates a unique key for a script name by converting it to lowercase.
    /// </summary>
    /// <param name="scriptName">The name of the script.</param>
    /// <returns>The normalized key for the script.</returns>
    public string GetKey(string scriptName)
    {
        return scriptName.ToLowerInvariant();
    }

    /// <summary>
    /// Returns the SHA256 hash of the supplied content
    /// </summary>
    /// <returns>The hash.</returns>
    /// <param name="content">Content.</param>
    public string GenerateHash(string content)
    {
        using var algorithm = SHA256.Create();

        return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    #endregion
}
