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

        var scriptKey = scriptSource.GetKey(options.DatabaseCreationScript!);

        var script = await scriptSource.GetScript(scriptKey, cancellationToken);

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

        var scriptKey = scriptSource.GetKey(options.DatabaseParametersScript!);

        var script = await scriptSource.GetScript(scriptKey, cancellationToken);

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
        logger.LogDebug("Validate table {VersionTableSchema}.{VersionTableName} exists",
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

        logger.LogDebug("Table {VersionTableSchema}.{VersionTableName} exists: {TableIsExists}",
            options.VersionTableSchema,
            options.VersionTableName,
            tableIsExists);

        return tableIsExists;
    }

    private async Task CreateVersionTable(CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating table {VersionTableSchema}.{VersionTableName}",
            options.VersionTableSchema,
            options.VersionTableName);

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

        script.Name = "Initialized";
        await InsertVersionTable(script, cancellationToken);
    }

    private async Task InsertVersionTable(Script script, CancellationToken cancellationToken)
    {
        logger.LogDebug("Add migration {MigrationName}", script.Name);

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
            { ParameterNames.ScriptName, script.Name! }
        };

        if (script.CanRepeat)
        {
            var contentsHash = scriptSource.GenerateHash(script.Content);
            parameters.Add(ParameterNames.ContentsHash, contentsHash);
        }
        else
        {
            parameters.Add(ParameterNames.ContentsHash, DBNull.Value);
        }

        command.CommandText = SetParameters(scriptInsertMigration.Content, parameters);

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogDebug("Added migration {MigrationName}, hash: {ContentsHash}",
            script.Name,
            parameters[ParameterNames.ContentsHash]);
        
    }

    private async Task<bool> DatabaseExists(CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking if database exists: {connectionString}", options.ConnectionString);

        await using var connection = new SqlConnection(options.ConnectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            logger.LogDebug("Checking database is exists");
            return true;
        }
        catch (SqlException)
        {
            logger.LogDebug("Checking database is not exists");
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
            await SetDatabaseParameters(cancellationToken);
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
