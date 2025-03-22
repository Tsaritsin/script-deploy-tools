IF OBJECT_ID (N'dbo.Migrations', N'U') IS NULL
BEGIN
    CREATE TABLE  dbo.Migrations (
        Id           INT IDENTITY (1, 1)
            CONSTRAINT PK_Migrations PRIMARY KEY CLUSTERED (Id ASC),
        ScriptKey   NVARCHAR(255) NOT NULL,
        Applied      DATETIME NOT NULL,
        ContentsHash NVARCHAR(255) NULL);
END
