namespace ScriptDeployTools.Tests;

/// <summary>
/// Unit tests for the <see cref="SortScriptsByDependenciesHelper"/> class.
/// Verifies sorting of scripts based on dependencies, including empty collections,
/// single scripts, dependency chains, and cyclic dependencies.
/// </summary>
public class SortScriptsByDependenciesHelperTests
{
    /// <summary>
    /// Verifies that sorting an empty collection of scripts results in an empty collection.
    /// </summary>
    [Fact]
    public void Sort_EmptyScripts_ReturnsEmptyCollection()
    {
        // Arrange
        var scripts = new Dictionary<string, Script>();
        var helper = new SortScriptsByDependenciesHelper();

        // Act
        var result = helper.Sort(scripts);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that sorting a collection containing a single script returns that same script unmodified.
    /// </summary>
    [Fact]
    public void Sort_SingleScript_ReturnsSingleScript()
    {
        // Arrange
        var scripts = new Dictionary<string, Script>
        {
            { "Script1", new Script("Script1", "Content1") }
        };
        var helper = new SortScriptsByDependenciesHelper();

        // Act
        var result = helper.Sort(scripts);

        // Assert
        Assert.Single(result);
        Assert.Equal("Script1", result.First().Key);
    }

    /// <summary>
    /// Tests that sorting a collection of scripts with dependencies produces the correct order,
    /// ensuring scripts appear only after all their dependencies.
    /// This test also includes scripts without any dependencies.
    /// </summary>
    [Fact]
    public void Sort_ScriptsWithDependencies_ReturnsSortedOrder()
    {
        // Arrange
        var scripts = new Dictionary<string, Script>
        {
            { "Script5", new Script("Script5", "Content5") { DependsOn = "Script4" } },
            { "Script3", new Script("Script3", "Content3") { DependsOn = "Script2" } },
            { "Script4", new Script("Script4", "Content4") { DependsOn = "Script2" } },
            { "Script2", new Script("Script2", "Content2") { DependsOn = "Script1" } },
            { "Script1", new Script("Script1", "Content1") },
            { "IndependentScript1", new Script("IndependentScript1", "IndependentContent1") },
            { "IndependentScript2", new Script("IndependentScript2", "IndependentContent2") }
        };
        var helper = new SortScriptsByDependenciesHelper();

        // Act
        var result = helper.Sort(scripts);

        // Assert
        var sortedKeys = result.Select(script => script.Key).ToList();

        Assert.Equal([
            "IndependentScript2",
            "IndependentScript1",
            "Script1",
            "Script2",
            "Script3",
            "Script4",
            "Script5"
        ], sortedKeys);
    }

    /// <summary>
    /// Ensures that attempting to sort scripts with cyclic dependencies throws
    /// an <see cref="CyclicDependencyException"/>.
    /// </summary>
    [Fact]
    public void Sort_CyclicDependencies_ThrowsException()
    {
        // Arrange
        var scripts = new Dictionary<string, Script>
        {
            { "Script1", new Script("Script1", "Content1") { DependsOn = "Script2" } },
            { "Script2", new Script("Script2", "Content2") { DependsOn = "Script3" } },
            { "Script3", new Script("Script3", "Content3") { DependsOn = "Script4" } },
            { "Script4", new Script("Script4", "Content4") { DependsOn = "Script2" } }
        };
        var helper = new SortScriptsByDependenciesHelper();

        // Act & Assert
        var exception = Assert.Throws<CyclicDependencyException>(() => helper.Sort(scripts));

        Assert.Equal("Cyclic dependency detected for script: Script2", exception.Message);


        Assert.Equal("Script2", exception.Node);
    }
}
