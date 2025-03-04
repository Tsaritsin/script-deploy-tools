namespace ScriptDeployTools;

/// <summary>
/// Represents the result of a deployment operation.
/// </summary>
public record DeploymentResult
{
    #region Methods

    /// <summary>
    /// Creates a successful deployment result.
    /// </summary>
    public static DeploymentResult Success(IDictionary<string, DeployScriptStatuses> deployScriptStatuses) =>
        new() { DeployScriptStatuses = deployScriptStatuses };

    /// <summary>
    /// Creates a deployment result indicating an error with the provided error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the deployment failure.</param>
    /// <returns>A <see cref="DeploymentResult"/> representing a failed deployment.</returns>
    public static DeploymentResult Error(string errorMessage) => new() { ErrorMessage = errorMessage };

    /// <summary>
    /// Creates a deployment result indicating an error with the provided error message.
    /// </summary>
    /// <param name="deployScriptStatuses"></param>
    /// <param name="errorMessage">The error message describing the deployment failure.</param>
    /// <returns>A <see cref="DeploymentResult"/> representing a failed deployment.</returns>
    public static DeploymentResult Error(IDictionary<string, DeployScriptStatuses> deployScriptStatuses,
                                         string errorMessage) => new()
    {
        ErrorMessage = errorMessage,
        DeployScriptStatuses = deployScriptStatuses
    };

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether the deployment was successful.
    /// </summary>
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets or sets the error message associated with the deployment, if any.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Holds the statuses of deployment results for each script key.
    /// Maps script keys to their respective deployment statuses.
    /// </summary>
    public IDictionary<string, DeployScriptStatuses> DeployScriptStatuses { get; init; } = new Dictionary<string, DeployScriptStatuses>();

    #endregion
}
