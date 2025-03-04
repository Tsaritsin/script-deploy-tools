using System.Reflection;
using System.Text;

namespace ScriptDeployTools.Sources.Embedded;

public class EmbeddedSourceOptions
{
    #region Assemblies

    private List<Assembly> _assemblies = [];

    public IReadOnlyCollection<Assembly> Assemblies
    {
        get => _assemblies;
        set => _assemblies = value.ToList();
    }

    #endregion

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
}
