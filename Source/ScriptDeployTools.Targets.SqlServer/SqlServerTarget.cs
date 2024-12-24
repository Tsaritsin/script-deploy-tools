using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ScriptDeployTools.Targets.SqlServer;

internal class SqlServerTarget(
    ILogger logger,
    IDeploySource scriptSource,
    SqlServerTargetOptions options) : IDeployTarget
{
    #region Fields

    private readonly EmbeddedScriptsHelper _embeddedScriptsHelper = new(logger);

    #endregion

    #region Methods

    private async Task CreateDatabase(CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating database: {connectionString}", options.ConnectionString);

        var builder = new SqlConnectionStringBuilder(options.ConnectionString)
        {
            InitialCatalog = "master"
        };

        var canCreateDatabase = !string.IsNullOrWhiteSpace(options.DatabaseCreationScript) &&
                                !string.IsNullOrWhiteSpace(options.DataPath) &&
                                !string.IsNullOrWhiteSpace(options.DefaultFilePrefix) &&
                                !string.IsNullOrWhiteSpace(options.DatabaseName);

        if (!canCreateDatabase)
            throw new InvalidOperationException("Database creation script is not specified");

        var directoryExists = Directory.Exists(options.DataPath);

        if (!directoryExists)
            throw new InvalidOperationException($"Data path '{options.DataPath}' does not exist");

        var scriptKey = EmbeddedScriptsHelper.GetKey(options.DatabaseCreationScript!);

        var script = await _embeddedScriptsHelper.GetScript(scriptKey, cancellationToken);

        if (script is null)
            throw new InvalidOperationException($"Database creation script '{options.DatabaseCreationScript}' is not found");

        await using var connection = new SqlConnection(builder.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = script.Content;

        command.Parameters.AddWithValue("DataPath", options.DataPath);
        command.Parameters.AddWithValue("DefaultFilePrefix", options.DefaultFilePrefix);
        command.Parameters.AddWithValue("DatabaseName", connection.Database);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<bool> VersionTableExists(CancellationToken cancellationToken)
    {
        var scriptKey = EmbeddedScriptsHelper.GetKey("TableExists");

        var script = await _embeddedScriptsHelper.GetScript(scriptKey, cancellationToken);

        if (script is null)
            throw new InvalidOperationException("TableExists script is not found");

        await using var connection = new SqlConnection(options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = script.Content;

        command.Parameters.AddWithValue("VersionTableSchema", options.VersionTableSchema);
        command.Parameters.AddWithValue("VersionTableName", options.VersionTableName);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToBoolean(result);
    }

    private async Task CreateVersionTable(CancellationToken cancellationToken)
    {
        var scriptKey = EmbeddedScriptsHelper.GetKey("InitializeVersionTable");

        var script = await _embeddedScriptsHelper.GetScript(scriptKey, cancellationToken);

        if (script is null)
            throw new InvalidOperationException("InitializeVersionTable script is not found");

        await using var connection = new SqlConnection(options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = script.Content;

        command.Parameters.AddWithValue("VersionTableSchema", options.VersionTableSchema);
        command.Parameters.AddWithValue("VersionTableName", options.VersionTableName);

        await command.ExecuteNonQueryAsync(cancellationToken);

        await InsertVersionTable(script, cancellationToken);
    }

    private async Task InsertVersionTable(Script script, CancellationToken cancellationToken)
    {
        var scriptKey = EmbeddedScriptsHelper.GetKey("InsertMigration");

        var scriptInsertMigration = await _embeddedScriptsHelper.GetScript(scriptKey, cancellationToken);

        if (scriptInsertMigration is null)
            throw new InvalidOperationException("InitializeVersionTable script is not found");

        await using var connection = new SqlConnection(options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = scriptInsertMigration.Content;

        command.Parameters.AddWithValue("VersionTableSchema", options.VersionTableSchema);
        command.Parameters.AddWithValue("VersionTableName", options.VersionTableName);
        command.Parameters.AddWithValue("ScriptName", script.Name);

        if (script.CanRepeat)
        {
            var contentsHash = scriptSource.GenerateHash(script.Content);
            command.Parameters.AddWithValue("ContentsHash", contentsHash);
        }
        else
        {
            command.Parameters.AddWithValue("ContentsHash", DBNull.Value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<bool> DatabaseExists(CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking if database exists: {connectionString}", options.ConnectionString);

        await using var connection = new SqlConnection(options.ConnectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    #endregion

    #region Implementation IDeployTarget

    public async Task PrepareToDeploy(CancellationToken cancellationToken)
    {
        var databaseExists = await DatabaseExists(cancellationToken);

        if (!databaseExists)
        {
            await CreateDatabase(cancellationToken);

            await CreateVersionTable(cancellationToken);
        }
        else
        {
            var tableExists = await VersionTableExists(cancellationToken);

            if (!tableExists)
            {
                await CreateVersionTable(cancellationToken);
            }
        }
    }

    public async Task Deploy(CancellationToken cancellationToken)
    {
        var scripts = await scriptSource.GetScripts(cancellationToken);
    }

    #endregion
}
