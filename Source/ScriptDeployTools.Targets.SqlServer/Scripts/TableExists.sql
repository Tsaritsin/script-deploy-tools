:SETVAR VersionTableSchema @VersionTableSchema
:SETVAR VersionTableName @VersionTableName

SELECT 1
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = '$(VersionTableName)'
  AND TABLE_SCHEMA = '$(VersionTableSchema)'
