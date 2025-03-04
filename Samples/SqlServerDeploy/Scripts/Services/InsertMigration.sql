INSERT dbo.Migrations(ScriptKey, Applied, ContentsHash)
VALUES(N'$(ScriptKey)', GETUTCDATE(), N'$(ContentsHash)')
