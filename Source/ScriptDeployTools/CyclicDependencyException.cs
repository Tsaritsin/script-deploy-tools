namespace ScriptDeployTools;

/// <summary>
/// Exception thrown when a cyclic dependency is detected during script processing.
/// </summary>
/// <param name="node">The name or key of the node where the cycle is detected.</param>
public class CyclicDependencyException(
    string node)
    : Exception(string.Format(DefaultMessage, node))
{
    /// <summary>
    /// Default error message template for cyclic dependencies.
    /// </summary>
    const string DefaultMessage = "Cyclic dependency detected for script: {0}";

    /// <summary>
    /// Gets the name or key of the node that caused the exception.
    /// </summary>
    public string Node { get; } = node;
}
