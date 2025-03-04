namespace ScriptDeployTools;

/// <summary>
/// Represents configuration options for the deployment process.
/// These options affect how migration scripts are handled
/// and whether registration of migrations is enabled or disabled.
/// </summary>
public class DeploymentOptions
{
    /// <summary>
    /// Gets or sets the script that will be used to insert migration metadata.
    /// </summary>
    public IScript? InsertMigrationScript { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the registration of migrations is disabled.
    /// If true, migration scripts will not be registered.
    /// </summary>
    public bool DisableRegistrationOfMigrations { get; set; }
}
