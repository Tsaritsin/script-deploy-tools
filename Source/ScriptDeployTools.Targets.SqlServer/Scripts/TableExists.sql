-- :SETVAR VersionTableSchema N''
-- :SETVAR VersionTableName N''

SELECT CAST(1 AS BIT) AS IsExists
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = '$(VersionTableName)'
  AND TABLE_SCHEMA = '$(VersionTableSchema)'
