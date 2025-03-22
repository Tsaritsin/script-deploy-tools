using SqlServerDeploy.Constants;
using SqlServerDeploy.Services;

namespace SqlServerDeploy.Scripts.v1_0_0.Database;

// /// <summary>
// ///     Script to set parameters of database
// /// </summary>
// internal record SetDatabaseParameters() : ScriptBase("SET_DATABASE_PARAMETERS")
// {
//     public override string DependsOn => "INITIALIZE_DATABASE";
//
//     public override int OrderGroup => -1000;
//
//     public override string ActualBefore => ScriptNames.InitializeVersion;
//
//     public override bool IsInitializeTarget => true;
//
//     public override string Source => "SqlServerDeploy.Scripts.v1_0_0.Database.SetDatabaseParameters.sql";
// }
