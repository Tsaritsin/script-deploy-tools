namespace ScriptDeployTools;

/// <summary>
/// Represents the various statuses of deploying a script.
/// </summary>
public enum DeployScriptStatuses
{
    /// <summary>
    /// NotDefined
    /// </summary>
    Unknown,

    /// <summary>
    /// The script is not up-to-date or relevant.
    /// </summary>
    NotActual,

    /// <summary>
    /// The content of the script is incorrect.
    /// </summary>
    WrongContent,

    /// <summary>
    /// The script has already been deployed.
    /// </summary>
    AlreadyDeployed,

    /// <summary>
    /// The script has been successfully deployed.
    /// </summary>
    Deployed,

    /// <summary>
    /// A dependency required for the script to deploy is missing.
    /// </summary>
    DependencyMissing,
}
