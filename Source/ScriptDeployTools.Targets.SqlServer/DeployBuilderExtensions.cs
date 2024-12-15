namespace ScriptDeployTools.Targets.SqlServer;

public static class DeployBuilderExtensions
{
    public static IDeployBuilder ToSqlServer(this IDeployBuilder builder,
                                             Action<SqlServerTargetOptions> applyOptions)
    {
        var options = new SqlServerTargetOptions();

        applyOptions(options);

        builder.Target = new SqlServerTarget(
            builder.Logger ?? throw new ArgumentNullException(nameof(builder.Logger)),
            builder.Source ?? throw new ArgumentNullException(nameof(builder.Source)),
            options);

        return builder;
    }
}
