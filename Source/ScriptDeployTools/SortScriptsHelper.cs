namespace ScriptDeployTools;

/// <summary>
///     Provides functionality to sort scripts based on their dependencies.
/// </summary>
public static class SortScriptsHelper
{
    /// <summary>
    ///     Sorts the provided scripts by their dependencies in a topological order.
    ///     Ensures that dependencies are resolved in the correct sequence.
    /// </summary>
    /// <param name="scripts">The dictionary of scripts to sort, keyed by their unique identifiers.</param>
    /// <returns>A read-only collection of sorted scripts in dependency order.</returns>
    public static IReadOnlyCollection<IScript> Sort(IReadOnlyCollection<IScript> scripts)
    {
        var helper = new SortScriptsByDependenciesHelper();

        var groupedScripts = scripts
            .Where(x => !x.IsService)
            .GroupBy(x => x.OrderGroup)
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.ToDictionary(y => y.ScriptKey));

        var sortedScripts = new List<IScript>();

        foreach (var group in groupedScripts.Keys)
        {
            var sortedGroup = helper.SortByDependencies(groupedScripts[group]);

            sortedScripts.AddRange(sortedGroup);
        }

        return sortedScripts;
    }
}

internal class SortScriptsByDependenciesHelper
{
    #region Fields

    private readonly Dictionary<string, List<string>> _graph = new();
    private readonly HashSet<string> _visited = [];
    private readonly HashSet<string> _visiting = [];
    private readonly Stack<string> _stack = new();

    #endregion

    #region Methods

    /// <summary>
    ///     Sorts a list of scripts by their dependencies using topological sorting.
    ///     Ensures that each script appears after the scripts it depends on.
    /// </summary>
    /// <param name="scripts">The list of scripts to sort.</param>
    /// <returns>A sorted list of scripts in dependency order.</returns>
    public IReadOnlyCollection<IScript> SortByDependencies(IDictionary<string, IScript> scripts)
    {
        Reset();

        LoadGraph(scripts.Values);

        // Sort nodes in graph
        foreach (var node in _graph.Keys)
        {
            if (_visited.Contains(node))
                continue;

            TopologicalSort(node);
        }

        // Get scripts by order from stack
        var sortedKeys = _stack.ToList();

        var sortedScripts = sortedKeys
            .Where(scripts.ContainsKey)
            .Select(key => scripts[key])
            .ToArray();

        return sortedScripts;
    }

    /// <summary>
    ///     Loads the graph structure from a list of script manifests.
    ///     Each script is represented as a node with edges to its dependent scripts.
    /// </summary>
    /// <param name="scripts">The list of script manifests to process.</param>
    private void LoadGraph(IEnumerable<IScript> scripts)
    {
        foreach (var script in scripts)
        {
            // Add current script
            _graph.TryAdd(script.ScriptKey, []);

            // If script has no dependencies, skip it
            if (string.IsNullOrEmpty(script.DependsOn))
                continue;

            // Add parent script and current script like dependence of parent
            _graph.TryAdd(script.DependsOn, []);

            _graph[script.DependsOn].Add(script.ScriptKey);
        }
    }

    /// <summary>
    ///     Performs a topological sort on the graph starting from the given node.
    ///     Throws an exception if a cycle is detected in the dependencies.
    /// </summary>
    /// <param name="node">The start node for the topological sort.</param>
    private void TopologicalSort(string node)
    {
        if (_visiting.Contains(node)) throw new CyclicDependencyException(node);

        if (_visited.Contains(node))
            return;

        // Push node to stack
        _visiting.Add(node);

        foreach (var neighbor in _graph[node])
            TopologicalSort(neighbor);

        _visiting.Remove(node);
        _visited.Add(node);
        _stack.Push(node);
    }

    /// <summary>
    ///     Resets the internal state of the helper, clearing the graph,
    ///     visited nodes, and other data structures.
    /// </summary>
    private void Reset()
    {
        _graph.Clear();
        _visited.Clear();
        _visiting.Clear();
        _stack.Clear();
    }

    #endregion
}
