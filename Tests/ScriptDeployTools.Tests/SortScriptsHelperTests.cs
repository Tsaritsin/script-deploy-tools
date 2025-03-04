using ScriptDeployTools.Tests.TestingModels;

namespace ScriptDeployTools.Tests;

/// <summary>
/// Unit tests for the <see cref="SortScriptsHelper"/> class.
/// Verifies sorting of scripts based on dependencies, including empty collections,
/// single scripts, dependency chains, and cyclic dependencies.
/// </summary>
public class SortScriptsHelperTests
{
    /// <summary>
    /// Verifies that sorting an empty collection of scripts results in an empty collection.
    /// </summary>
    [Fact]
    public void Sort_EmptyScripts_ReturnsEmptyCollection()
    {
        // Act
        var result = SortScriptsHelper.Sort(Array.Empty<IScript>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that sorting a collection containing a service script returns without that.
    /// </summary>
    [Fact]
    public void Sort_ExcludeServiceScripts_ReturnsNotServiceScript()
    {
        // Arrange
        var scripts = new List<IScript>
        {
            new ScriptTesting("Script1"),
            new ScriptTesting("Script2") { IsService = true }
        };

        // Act
        var result = SortScriptsHelper.Sort(scripts);

        // Assert
        Assert.Single(result);
        Assert.Equal("Script1", result.First().ScriptKey);
    }

    /// <summary>
    /// Verifies that sorting a collection containing a single script returns that same script unmodified.
    /// </summary>
    [Fact]
    public void Sort_SingleScript_ReturnsSingleScript()
    {
        // Arrange
        var scripts = new List<IScript>
        {
            new ScriptTesting("Script1")
        };

        // Act
        var result = SortScriptsHelper.Sort(scripts);

        // Assert
        Assert.Single(result);
        Assert.Equal("Script1", result.First().ScriptKey);
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
        var scripts = new List<IScript>
        {
            new ScriptTesting("Script6") { DependsOn = "Script2", OrderGroup = 0 },
            new ScriptTesting("Script5") { DependsOn = "Script4", OrderGroup = 0 },
            new ScriptTesting("Script3") { DependsOn = "Script2", OrderGroup = 0 },
            new ScriptTesting("Script4") { DependsOn = "Script2", OrderGroup = 0 },
            new ScriptTesting("Script2") { DependsOn = "Script1", OrderGroup = 0 },
            new ScriptTesting("Script1") { OrderGroup = 0 },
            new ScriptTesting("IndependentScript1") { OrderGroup = -1000 },
            new ScriptTesting("IndependentScript2") { OrderGroup = -1000 }
        };

        // Act
        var result = SortScriptsHelper.Sort(scripts);

        // Assert
        var sortedKeys = result.Select(script => script.ScriptKey).ToList();

        Assert.Equal([
            "IndependentScript2",
            "IndependentScript1",
            "Script1",
            "Script2",
            "Script4",
            "Script5",
            "Script3",
            "Script6",
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
        var scripts = new List<IScript>
        {
            new ScriptTesting("Script1") { DependsOn = "Script2" },
            new ScriptTesting("Script2") { DependsOn = "Script3" },
            new ScriptTesting("Script3") { DependsOn = "Script4" },
            new ScriptTesting("Script4") { DependsOn = "Script2" }
        };

        // Act & Assert
        var exception = Assert.Throws<CyclicDependencyException>(() => SortScriptsHelper.Sort(scripts));

        Assert.Equal("Cyclic dependency detected for script: Script2", exception.Message);


        Assert.Equal("Script2", exception.Node);
    }
}
