﻿USE [master]

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN    

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
        COLLATE SQL_Latin1_General_CP1_CI_AS;
    
    ALTER DATABASE [$(DatabaseName)] SET
        ANSI_NULLS ON,
        ANSI_PADDING ON,
        ANSI_WARNINGS ON,
        ARITHABORT ON,
        AUTO_CLOSE OFF,
        AUTO_SHRINK OFF,
        AUTO_UPDATE_STATISTICS ON,
        CURSOR_CLOSE_ON_COMMIT OFF,
        CURSOR_DEFAULT  LOCAL,
        CONCAT_NULL_YIELDS_NULL ON,
        NUMERIC_ROUNDABORT OFF,
        QUOTED_IDENTIFIER ON,
        RECURSIVE_TRIGGERS OFF,
        DISABLE_BROKER,
        AUTO_UPDATE_STATISTICS_ASYNC OFF,
        DATE_CORRELATION_OPTIMIZATION OFF,
        TRUSTWORTHY OFF,
        ALLOW_SNAPSHOT_ISOLATION OFF;
    
    ALTER DATABASE [$(DatabaseName)] SET
        PARAMETERIZATION SIMPLE,
        READ_COMMITTED_SNAPSHOT OFF,
        HONOR_BROKER_PRIORITY OFF,
        RECOVERY FULL,
        MULTI_USER,
        PAGE_VERIFY NONE,
        DB_CHAINING OFF,
        READ_WRITE;
END
