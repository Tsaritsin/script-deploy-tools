namespace ScriptDeployTools.Sources.Embedded;

public static class DeployBuilderExtensions
{
    public static IDeployBuilder FromEmbeddedResources(this IDeployBuilder builder,
                                                       Action<EmbeddedSourceOptions> applyOptions)
    {
        var options = new EmbeddedSourceOptions();

        applyOptions(options);

        ArgumentException.ThrowIfNullOrWhiteSpace(options.ScriptExtension);
        ArgumentNullException.ThrowIfNull(builder.Logger);

        builder.Source = new EmbeddedSource(builder.Logger, options);

        return builder;
    }
}
