using Microsoft.Data.SqlClient;

namespace SqlServerDeploy.Services;

internal static class ConnectionStringHelper
{
    public static string GetConnectionStringBySettings(DeploySettings settings)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = settings.DataSource,
            InitialCatalog = settings.DatabaseName,
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
