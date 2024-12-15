using System.Reflection;
using System.Text;

namespace ScriptDeployTools.Sources.Embedded;

public class EmbeddedSourceOptions
{
    public IReadOnlyCollection<Assembly>? Assemblies { get; set; }

    #region Filter

    private Func<string, bool>? _filter;

    public Func<string, bool> Filter
    {
        get => _filter ?? (_ => true);
        set => _filter = value;
    }

    #endregion

    #region Encoding

    private Encoding? _encoding;

    public Encoding Encoding
    {
        get => _encoding ??= Encoding.UTF8;
        set => _encoding = value;
    }

    #endregion

    public string ManifestExtension { get; set; } = ".json";

    public string? ScriptExtension { get; set; }
}
