namespace ScriptDeployTools;

/// <summary>
/// Initializes a new instance of the <see cref="Script"/> class.
/// </summary>
/// <param name="key">The unique identifier for the script.</param>
/// <param name="content">The content of the script.</param>
public class Script(
    string key,
    string content)
{
    /// <summary>
    /// Gets the name of the script.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Gets the content of the script.
    /// </summary>
    /// <value></value>

    public string Content { get; } = content;

    /// <summary>
    /// Key of parent script
    /// </summary>
    public string? DependsOn { get; set; }

    /// <summary>
    /// Key of deployed script after that current script become not actual and can not deploy
    /// </summary>
    public string? ActualBefore { get; set; }

    /// <summary>
    /// Description of action in scrip
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Means script will repeat when changed hash
    /// </summary>
    public bool CanRepeat { get; set; }

    /// <summary>
    /// Hash of content
    /// </summary>
    public string? ContentsHash { get; set; }
}
