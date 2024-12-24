-- :SETVAR VersionTableSchema N''
-- :SETVAR VersionTableName N''

CREATE TABLE $(VersionTableSchema).$(VersionTableName) (
    ScriptName   NVARCHAR(255) NOT NULL,
    Applied      DATETIME NOT NULL,
    ContentsHash NVARCHAR(255) NULL)
