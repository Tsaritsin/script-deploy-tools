:SETVAR VersionTableSchema @VersionTableSchema
:SETVAR VersionTableName @VersionTableName
:SETVAR ScriptName @ScriptName
:SETVAR Applied @Applied
:SETVAR ContentsHash @ContentsHash

INSERT $(VersionTableSchema).$(VersionTableName)(ScriptName, Applied, ContentsHash)
VALUES($(ScriptName), GETUTCDATE(), $(ContentsHash))
GO
