-- :SETVAR VersionTableSchema N''
-- :SETVAR VersionTableName N''

CREATE TABLE $(VersionTableSchema).$(VersionTableName) (
    Id           INT IDENTITY (1, 1)
        CONSTRAINT PK_$(VersionTableName) PRIMARY KEY CLUSTERED (Id ASC),
    ScriptName   NVARCHAR(255) NOT NULL,
    Applied      DATETIME NOT NULL,
    ContentsHash NVARCHAR(255) NULL)
