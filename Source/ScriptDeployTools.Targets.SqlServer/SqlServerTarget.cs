using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ScriptDeployTools.Targets.SqlServer;

internal class SqlServerTarget(
    ILogger logger,
    IDeploySource scriptSource,
    SqlServerTargetOptions options) : IDeployTarget
{
    private readonly EmbeddedScriptsHelper _embeddedScriptsHelper = new(logger);

    public async Task PrepareToDeploy(CancellationToken cancellationToken)
    {
        var databaseExists = await DatabaseExists(
            options.ConnectionString ?? throw new ArgumentNullException(nameof(options.ConnectionString)));

        if (!databaseExists)
        {
            var builder = new SqlConnectionStringBuilder(options.ConnectionString)
            {
                InitialCatalog = "master"
            };

            await CreateDatabase(builder.ConnectionString, cancellationToken);

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
        var scripts = await scriptSource.GetScripts();
    }

    private async Task CreateDatabase(string connectionString, CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating database: {connectionString}", connectionString);

        var canCreateDatabase = !string.IsNullOrWhiteSpace(options.DatabaseCreationScript) &&
                                !string.IsNullOrWhiteSpace(options.DataPath) &&
                                !string.IsNullOrWhiteSpace(options.DefaultFilePrefix) &&
                                !string.IsNullOrWhiteSpace(options.DatabaseName);

        if (!canCreateDatabase)
            throw new InvalidOperationException("Database creation script is not specified");

        var directoryExists = Directory.Exists(options.DataPath);

        if (!directoryExists)
            throw new InvalidOperationException($"Data path '{options.DataPath}' does not exist");

        var script = await scriptSource.GetScript(options.DatabaseCreationScript!);

        if (script is null)
            throw new InvalidOperationException($"Database creation script '{options.DatabaseCreationScript}' is not found");

        await using var connection = new SqlConnection(connectionString);

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
        var script = await _embeddedScriptsHelper.GetScript("TableExists");

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
        var script = await _embeddedScriptsHelper.GetScript("InitializeVersionTable");

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
        var scriptInsertMigration = await _embeddedScriptsHelper.GetScript("InsertMigration");

        if (scriptInsertMigration is null)
            throw new InvalidOperationException("InitializeVersionTable script is not found");

        await using var connection = new SqlConnection(options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = scriptInsertMigration.Content;

        command.Parameters.AddWithValue("VersionTableSchema", options.VersionTableSchema);
        command.Parameters.AddWithValue("VersionTableName", options.VersionTableName);
        command.Parameters.AddWithValue("ScriptName", script.Manifest!.Name);

        if (script.Manifest!.CanRepeat)
        {
            var contentsHash = GenerateHash(script.Content);
            command.Parameters.AddWithValue("ContentsHash", contentsHash);
        }
        else
        {
            command.Parameters.AddWithValue("ContentsHash", DBNull.Value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the SHA256 hash of the supplied content
    /// </summary>
    /// <returns>The hash.</returns>
    /// <param name="content">Content.</param>
    private static string GenerateHash(string content)
    {
        using var algorithm = SHA256.Create();

        return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    private async Task<bool> DatabaseExists(string connectionString)
    {
        logger.LogDebug("Checking if database exists: {connectionString}", connectionString);

        await using var connection = new SqlConnection(connectionString);

        try
        {
            connection.Open();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }
}
