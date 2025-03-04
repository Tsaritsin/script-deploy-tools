using ScriptDeployTools;

namespace SqlServerDeploy.Services;

internal abstract record ScriptBase(
        string ScriptKey) : IScript
{
        public abstract string? DependsOn { get; }

        public string? Content { get; set; }

        public virtual int OrderGroup => 0;

        public virtual bool IsService => false;

        public virtual string? ActualBefore => null;

        public virtual bool CanRepeat => false;

        public string? ContentsHash { get; set; }

        public abstract string Source { get; }

        public virtual bool IsInitializeTarget => false;

        public virtual IDictionary<string, string?> ScriptParameters => new Dictionary<string, string?>();
}
