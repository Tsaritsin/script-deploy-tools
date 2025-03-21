# ScriptDeployTools
Inspired by [DbUp](https://github.com/DbUp/DbUp) supporting easy filtering, ordering and versioning:

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Tsaritsin/script-deploy-tools/tagged.yml)

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

## OrderGroup
Deploying by groups in special sequences

## IsService
This script not sorting and deployed only in special code (e.g. insert migrations)

## IsInitializeTarget
This scripts can deploy when target is not created (e.g. script to create database).

## Define script

Used class (implementation of IScript) for describe script's properties: 
```csharp
internal record DeviceTypes() : ScriptBase("DEVICE_TYPES")
{
    public override string DependsOn => "IDENTITY_COMMON";

    public override string Source => "SqlServerDeploy.Scripts.v1_0_0.Tables.DeviceTypes.sql";
}
```
Script's content stored in resources by path from properties "Source" (implemented for package
ScriptDeployTools.Sources.Embedded).

## Used
```csharp
var deployService = new DeployBuilder()
    .AddLogger(loggerFactory.CreateLogger<IDeploymentService>())
    .AddOptions(new DeploymentOptions
    {
        InsertMigrationScript = scripts.FirstOrDefault(x =>
            x is { IsService: true, ScriptKey: "INSERT_MIGRATION" })
    })
    .FromEmbeddedResources(options =>
    {
        options.Assemblies = [typeof(DeployHelper).Assembly];
    })
    .ToSqlServer(options =>
    {
        options.ConnectionString = connectionString;
        options.GetDeployedInfoScript = scripts.FirstOrDefault(x =>
            x is { IsService: true, ScriptKey: "GET_DEPLOYED_SCRIPTS" });
    })
    .Build();

await deployService.Deploy(scripts, cancellationToken);
```
A more detailed example is available [in this repository](https://github.com/Tsaritsin/script-deploy-tools/tree/main/Samples/SqlServerDeploy).
