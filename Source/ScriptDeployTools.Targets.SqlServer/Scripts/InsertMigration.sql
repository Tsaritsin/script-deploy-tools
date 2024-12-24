-- :SETVAR VersionTableSchema N''
-- :SETVAR VersionTableName N''
-- :SETVAR ScriptName N''
-- :SETVAR ContentsHash N''

INSERT $(VersionTableSchema).$(VersionTableName)(ScriptName, Applied, ContentsHash)
VALUES(N'$(ScriptName)', GETUTCDATE(), N'$(ContentsHash)')
