-- :SETVAR DataPath N''
-- :SETVAR DatabaseName N''
-- :SETVAR DefaultFilePrefix N''

CREATE DATABASE [$(DatabaseName)]
    ON PRIMARY (
        NAME = Data,
        FILENAME = N'$(DataPath)$(DefaultFilePrefix).mdf',
        SIZE = 102400 KB,
        FILEGROWTH = 102400 KB)
    LOG ON (
        NAME = Logs,
        FILENAME = N'$(DataPath)$(DefaultFilePrefix).ldf',
        SIZE = 25600 KB,
        FILEGROWTH = 10 %)
    COLLATE SQL_Latin1_General_CP1_CI_AS
