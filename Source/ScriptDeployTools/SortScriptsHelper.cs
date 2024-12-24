namespace ScriptDeployTools;

internal class SortScriptsByDependenciesHelper
{
    #region Fields

    private readonly Dictionary<string, List<string>> _graph = new();
    private readonly HashSet<string> _visited = [];
    private readonly HashSet<string> _visiting = [];
    private readonly Stack<string> _stack = new();

    #endregion

    #region Methods

    public static IReadOnlyCollection<Script> Sort(IDictionary<string, Script> scripts)
    {
        var helper = new SortScriptsByDependenciesHelper();

        return helper.SortScriptsByDependencies(scripts);
    }

    /// <summary>
    ///     Loads the graph structure from a list of script manifests.
    ///     Each script is represented as a node with edges to its dependent scripts.
    /// </summary>
    /// <param name="scripts">The list of script manifests to process.</param>
    private void LoadGraph(IEnumerable<Script> scripts)
    {
        foreach (var script in scripts)
        {
            // Add current script
            _graph.TryAdd(script.Key, []);

            // If script has no dependencies, skip it
            if (string.IsNullOrEmpty(script.DependsOn))
                continue;

            // Add parent script and current script like dependence of parent
            _graph.TryAdd(script.DependsOn, []);

            _graph[script.DependsOn].Add(script.Key);
        }
    }

    /// <summary>
    ///     Performs a topological sort on the graph starting from the given node.
    ///     Throws an exception if a cycle is detected in the dependencies.
    /// </summary>
    /// <param name="node">The start node for the topological sort.</param>
    private void TopologicalSort(string node)
    {
        if (_visiting.Contains(node)) throw new InvalidOperationException($"Detected cycle for: {node}");

        if (_visited.Contains(node))
            return;

        // Push node to stack
        _visiting.Add(node);

        foreach (var neighbor in _graph[node]) TopologicalSort(neighbor);

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

    /// <summary>
    ///     Sorts a list of script manifests by their dependencies using topological sorting.
    ///     Ensures that each script appears after the scripts it depends on.
    /// </summary>
    /// <param name="scripts">The list of script manifests to sort.</param>
    /// <returns>A sorted list of script manifests in dependency order.</returns>
    public IReadOnlyCollection<Script> SortScriptsByDependencies(IDictionary<string, Script> scripts)
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
        var orderedKeys = _stack.ToList();

        return orderedKeys
            .Select(item => scripts[item])
            .ToArray();
    }

    #endregion
}
