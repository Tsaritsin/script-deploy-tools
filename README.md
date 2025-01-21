# ScriptDeployTools
Inspired by [DbUp](https://github.com/DbUp/DbUp) supporting easy filtering, ordering and versioning:

## Key
Not deployed if a script with the same key is already deployed.

## DependsOn
Not deployed unless a script with this key is deployed.

## ActualBefore
Not deployed if a script with this key is already deployed.

## CanRepeat and ContentsHash
It is deployed every time if the hash has changed.

Used extensions are implemented `IDeploySource` and `IDeployTarget`
for different sources and targets.

## Used
```csharp
var deployService = new DeployBuilder()
    .AddLogger(loggerFactory.CreateLogger<IDeploymentService>())
    .FromEmbeddedResources(options =>
    {
        options.Assemblies = [typeof(DeployHelper).Assembly];
        options.ScriptExtension = ".sql";
    })
    .ToSqlServer(options =>
    {
        options.ConnectionString = connectionString;
        options.DatabaseCreationScript = "INITIALIZE_DATABASE";
        options.DatabaseParametersScript = "SET_DATABASE_PARAMETERS";
        options.DatabaseName = deploySettings.DatabaseName;
        options.DefaultFilePrefix = deploySettings.DefaultFilePrefix;
        options.DataPath = deploySettings.DataPath;
    })
    .Build();

await deployService.Deploy(cancellationToken);

```
A more detailed example is available [in this repository](https://github.com/Tsaritsin/script-deploy-tools/tree/main/Samples/SqlServerDeploy).
