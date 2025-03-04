namespace ScriptDeployTools;

/// <summary>
/// Script to deploy
/// </summary>
public interface IScript
{
    /// <summary>
    /// Gets the key of the script.
    /// </summary>
    string ScriptKey { get; }

    /// <summary>
    /// Key of parent script, used to sorting in Deployment service
    /// Also in targets can check already deployed parent script
    /// </summary>
    string? DependsOn { get; }

    /// <summary>
    /// Gets the content of the script.
    /// </summary>
    string? Content { get; set; }

    /// <summary>
    /// Deployment order of scripts
    /// </summary>
    int OrderGroup { get; }

    /// <summary>
    /// Script not for deployment - it is service
    /// </summary>
    bool IsService { get; }

    /// <summary>
    /// Key of deployed script after that current script become not actual and can not deploy
    /// </summary>
    string? ActualBefore { get; }

    /// <summary>
    /// Means script will repeat when changed hash
    /// </summary>
    bool CanRepeat { get; }

    /// <summary>
    /// Hash of content
    /// </summary>
    string? ContentsHash { get; set; }

    /// <summary>
    /// Script is using for initialize target (e.g. create database)
    /// </summary>
    bool IsInitializeTarget { get; }

    /// <summary>
    /// Data is using for get content of script from source (e.g. resource name)
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Name and value of parameters
    /// </summary>
    IDictionary<string, string?> ScriptParameters { get; }
}
