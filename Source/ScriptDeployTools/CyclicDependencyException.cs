namespace ScriptDeployTools;

public class CyclicDependencyException(
    string node)
    : Exception(string.Format(DefaultMessage, node))
{
    const string DefaultMessage = "Cyclic dependency detected for script: {0}";

    public string Node { get; } = node;
}
