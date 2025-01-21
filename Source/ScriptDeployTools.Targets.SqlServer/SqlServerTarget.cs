using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ScriptDeployTools.Targets.SqlServer.Constants;

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

    private static IDictionary<string, string> TranslateParameterToSql(IDictionary<string, object> parameters)
    {
        var result = new Dictionary<string, string>();

        foreach (var parameter in parameters)
        {
            var value = parameter.Value switch
            {
                string stringValue => stringValue,
                DBNull => "NULL",
                _ => "NULL"
            };

            result.Add(parameter.Key, value);
        }

        return result;
    }

    private static string SetParameters(string script,
                                        IDictionary<string, object> parameters)
    {
        var sqlParameters = TranslateParameterToSql(parameters);

        return sqlParameters.Aggregate(
            script,
            (current, parameter) => current.Replace($"$({parameter.Key})", parameter.Value));
    }

    private async Task CreateDatabase(CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating database: {DatabaseName}", options.DatabaseName);

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

        var script = await scriptSource.GetScript(options.DatabaseCreationScript!, cancellationToken);

        if (script is null)
            throw new InvalidOperationException($"Database creation script '{options.DatabaseCreationScript}' is not found");

        await using var connection = new SqlConnection(builder.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = SetParameters(script.Content, new Dictionary<string, object>
        {
            { ParameterNames.DataPath, options.DataPath! },
            { ParameterNames.DefaultFilePrefix, options.DefaultFilePrefix! },
            { ParameterNames.DatabaseName, options.DatabaseName! }
        });

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogDebug("Created database {DatabaseName}", options.DatabaseName);
    }

    private async Task SetDatabaseParameters(CancellationToken cancellationToken)
    {
        logger.LogDebug("Set database parameters");

        var canExecute = !string.IsNullOrWhiteSpace(options.DatabaseParametersScript);

        if (!canExecute)
            return;

        var script = await scriptSource.GetScript(options.DatabaseParametersScript!, cancellationToken);

        if (script is null)
            return;

        var builder = new SqlConnectionStringBuilder(options.ConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(builder.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = SetParameters(script.Content, new Dictionary<string, object>
        {
            { ParameterNames.DatabaseName, options.DatabaseName! }
        });

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogDebug("Set database parameters completed");
    }

    private async Task<bool> VersionTableExists(CancellationToken cancellationToken)
    {
        logger.LogDebug("Validate table {VersionTableSchema}.{VersionTableName}",
            options.VersionTableSchema,
            options.VersionTableName);

        var scriptKey = EmbeddedScriptsHelper.GetKey(ScriptNames.TableExists);

        var script = await _embeddedScriptsHelper.GetScript(scriptKey, cancellationToken);

        if (script is null)
            throw new InvalidOperationException("TableExists script is not found");

        await using var connection = new SqlConnection(options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = SetParameters(script.Content, new Dictionary<string, object>
        {
            { ParameterNames.VersionTableSchema, options.VersionTableSchema },
            { ParameterNames.VersionTableName, options.VersionTableName }
        });

        var result = await command.ExecuteScalarAsync(cancellationToken);

        var tableIsExists = Convert.ToBoolean(result);

        logger.LogDebug("Table {VersionTableSchema}.{VersionTableName} is exists: {TableIsExists}",
            options.VersionTableSchema,
            options.VersionTableName,
            tableIsExists);

        return tableIsExists;
    }

    private async Task CreateVersionTable(CancellationToken cancellationToken)
    {
        var scriptKey = EmbeddedScriptsHelper.GetKey(ScriptNames.InitializeVersionTable);

        var script = await _embeddedScriptsHelper.GetScript(scriptKey, cancellationToken);

        if (script is null)
            throw new InvalidOperationException("InitializeVersionTable script is not found");

        await using var connection = new SqlConnection(options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = SetParameters(script.Content, new Dictionary<string, object>
        {
            { ParameterNames.VersionTableSchema, options.VersionTableSchema },
            { ParameterNames.VersionTableName, options.VersionTableName }
        });

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogDebug("Created table {VersionTableSchema}.{VersionTableName}",
            options.VersionTableSchema,
            options.VersionTableName);
    }

    private async Task InsertToVersionTable(Script script, CancellationToken cancellationToken)
    {
        var scriptKey = EmbeddedScriptsHelper.GetKey(ScriptNames.InsertMigration);

        var scriptInsertMigration = await _embeddedScriptsHelper.GetScript(scriptKey, cancellationToken);

        if (scriptInsertMigration is null)
            throw new InvalidOperationException("InsertMigration script is not found");

        await using var connection = new SqlConnection(options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        var parameters = new Dictionary<string, object>()
        {
            { ParameterNames.VersionTableSchema, options.VersionTableSchema },
            { ParameterNames.VersionTableName, options.VersionTableName },
            { ParameterNames.ScriptName, script.Key! }
        };

        if (script.CanRepeat)
        {
            var contentsHash = GenerateHash(script.Content);
            parameters.Add(ParameterNames.ContentsHash, contentsHash);
        }
        else
        {
            parameters.Add(ParameterNames.ContentsHash, DBNull.Value);
        }

        command.CommandText = SetParameters(scriptInsertMigration.Content, parameters);

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogDebug("Added migration {MigrationName}, hash: {ContentsHash}",
            script.Key,
            parameters[ParameterNames.ContentsHash]);
    }

    private async Task<bool> DatabaseExists(CancellationToken cancellationToken)
    {
        logger.LogDebug("Connecting to database: {connectionString}", options.ConnectionString);

        await using var connection = new SqlConnection(options.ConnectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            logger.LogDebug("Database is exists");
            return true;
        }
        catch (SqlException)
        {
            logger.LogDebug("Connecting failed, database is not exists");
            return false;
        }
    }

    /// <summary>
    /// Returns the SHA256 hash of the supplied content
    /// </summary>
    /// <returns>The hash.</returns>
    /// <param name="content">Content.</param>
    public string GenerateHash(string content)
    {
        using var algorithm = SHA256.Create();

        return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    #endregion

    #region Implementation IDeployTarget

    public async Task PrepareToDeploy(CancellationToken cancellationToken)
    {
        var databaseExists = await DatabaseExists(cancellationToken);

        if (!databaseExists)
        {
            await CreateDatabase(cancellationToken);
            await SetDatabaseParameters(cancellationToken);
            await CreateVersionTable(cancellationToken);

            await InsertToVersionTable(
                new Script(options.DatabaseCreationScript!, string.Empty),
                cancellationToken);

            await InsertToVersionTable(
                new Script(options.DatabaseParametersScript!, string.Empty),
                cancellationToken);

            await InsertToVersionTable(
                new Script(ScriptDeployTools.Constants.RootScript.Name, string.Empty),
                cancellationToken);
        }
        else
        {
            var tableExists = await VersionTableExists(cancellationToken);

            if (!tableExists)
            {
                await CreateVersionTable(cancellationToken);

                await InsertToVersionTable(
                    new Script(ScriptDeployTools.Constants.RootScript.Name, string.Empty),
                    cancellationToken);
            }
        }
    }

    public async Task<List<ScriptDeployed>> GetDeployedScripts(CancellationToken cancellationToken)
    {
        logger.LogDebug("Get applied migrations");

        var scriptKey = EmbeddedScriptsHelper.GetKey(ScriptNames.GetDeployedScripts);

        var script = await _embeddedScriptsHelper.GetScript(scriptKey, cancellationToken);

        if (script is null)
            throw new InvalidOperationException("GetDeployedScripts script is not found");

        await using var connection = new SqlConnection(options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = SetParameters(script.Content, new Dictionary<string, object>
        {
            { ParameterNames.VersionTableSchema, options.VersionTableSchema },
            { ParameterNames.VersionTableName, options.VersionTableName }
        });

        var result = new List<ScriptDeployed>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ScriptDeployed(reader.FromDb("ScriptName", string.Empty))
            {
                ContentsHash = reader.FromDb<string>("ContentsHash")
            });
        }

        logger.LogDebug("Found {CountAppliedMigrations} applied migrations", result.Count);

        return result;
    }

    public async Task DeployScript(Script script, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deploy {Script}", script.Key);

        await using var connection = new SqlConnection(options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = script.Content;

        await command.ExecuteNonQueryAsync(cancellationToken);

        script.ContentsHash = GenerateHash(script.Content);

        await InsertToVersionTable(script, cancellationToken);
    }

    #endregion
}
