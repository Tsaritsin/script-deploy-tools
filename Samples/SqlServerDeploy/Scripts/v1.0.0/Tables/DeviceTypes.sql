CREATE TABLE dbo.DeviceTypes (
    Id          INT NOT NULL
        CONSTRAINT PK_DeviceTypes PRIMARY KEY CLUSTERED (Id ASC)
        CONSTRAINT DF_DeviceTypes_Id DEFAULT NEXT VALUE FOR dbo.IDENTITY_Common,
    Name        NVARCHAR(50) NOT NULL,
    Description NVARCHAR(1000) NULL)

