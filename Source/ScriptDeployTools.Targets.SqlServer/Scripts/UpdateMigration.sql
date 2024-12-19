:SETVAR VersionTableSchema @VersionTableSchema
:SETVAR VersionTableName @VersionTableName
:SETVAR ScriptName @ScriptName
:SETVAR Applied @Applied
:SETVAR ContentsHash @ContentsHash

UPDATE $(VersionTableSchema).$(VersionTableName)
SET Applied = GETUTCDATE(),
    ContentsHash = $(ContentsHash)
WHERE ScriptName = $(ScriptName)
GO
