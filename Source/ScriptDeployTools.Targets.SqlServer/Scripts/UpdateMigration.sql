-- :SETVAR VersionTableSchema N''
-- :SETVAR VersionTableName N''
-- :SETVAR ScriptName N''
-- :SETVAR ContentsHash N''

UPDATE $(VersionTableSchema).$(VersionTableName)
SET Applied = GETUTCDATE(),
    ContentsHash = N'$(ContentsHash)'
WHERE ScriptName = N'$(ScriptName)'
