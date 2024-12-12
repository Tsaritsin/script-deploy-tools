using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ScriptDeployTools.SqlServer;

internal class SqlServerTarget(
    ILogger logger,
    IDeploySource scriptSource,
    SqlServerTargetOptions options) : IDeployTarget
{
    public async Task PrepareToDeploy()
    {
        var databaseExists = await DatabaseExists(
            options.ConnectionString ?? throw new ArgumentNullException(nameof(options.ConnectionString)));

        if (databaseExists)
            return;

        var builder = new SqlConnectionStringBuilder(options.ConnectionString)
        {
            InitialCatalog = "master"
        };

        await CreateDatabase(builder.ConnectionString);
    }

    private async Task CreateDatabase(string connectionString)
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

        var scriptIsFound = !string.IsNullOrWhiteSpace(script);

        if (!scriptIsFound)
            throw new InvalidOperationException($"Database creation script '{options.DatabaseCreationScript}' is not found");

        await using var connection = new SqlConnection(connectionString);

        try
        {
            connection.Open();

            await using var command = connection.CreateCommand();

            command.CommandText = script!;

            command.Parameters.AddWithValue("@DataPath", options.DataPath);
            command.Parameters.AddWithValue("@DefaultFilePrefix", options.DefaultFilePrefix);
            command.Parameters.AddWithValue("@DatabaseName", connection.Database);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to create database");
        }
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
