﻿using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SqlServerDeploy.Constants.Infrastructure;

namespace SqlServerDeploy.Services.ConnectionString;

internal static class ConnectionStringHelper
{
    private static readonly ConnectionStringSettings Settings = new();
    private static bool _settingsIsLoaded;

    private static ConnectionStringSettings GetSettings(IConfiguration configuration)
    {
        if (!_settingsIsLoaded)
        {
            configuration
                .GetSection(ConnectionStrings.DatabaseConnectionSettings)
                .Bind(Settings);

            _settingsIsLoaded = true;
        }

        if (Settings is null) throw new ApplicationException("Connection string settings is missing or invalid.");

        return Settings;
    }

    public static string GetConnectionString(this IConfiguration configuration)
    {
        var settings = GetSettings(configuration);

        return GetConnectionStringBySettings(settings);
    }

    private static string GetConnectionStringBySettings(ConnectionStringSettings settings)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = settings.DataSource,
            InitialCatalog = settings.InitialCatalog,
            IntegratedSecurity = settings.IntegratedSecurity,
            Pooling = false
        };

        if (!builder.IntegratedSecurity)
        {
            builder.UserID = settings.User;
            builder.Password = settings.Password;
        }

        builder.Encrypt = false;
        builder.TrustServerCertificate = true;

        return builder.ConnectionString;
    }
}
