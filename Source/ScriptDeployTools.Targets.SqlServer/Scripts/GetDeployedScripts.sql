-- :SETVAR VersionTableSchema N''
-- :SETVAR VersionTableName N''

SELECT ScriptName,
       ContentsHash
FROM $(VersionTableSchema).$(VersionTableName)

