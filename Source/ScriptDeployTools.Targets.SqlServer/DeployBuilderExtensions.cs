namespace ScriptDeployTools.Targets.SqlServer;

public static class DeployBuilderExtensions
{
    public static IDeployBuilder ToSqlServer(this IDeployBuilder builder,
                                             Action<SqlServerTargetOptions> applyOptions)
    {
        var options = new SqlServerTargetOptions();

        applyOptions(options);

        ArgumentNullException.ThrowIfNull(options.ConnectionString);
        ArgumentNullException.ThrowIfNull(options.GetDeployedInfoScript);

        builder.Target = new SqlServerTarget(
            builder.Logger ?? throw new ArgumentNullException(nameof(builder.Logger)),
            options);

        return builder;
    }
}
