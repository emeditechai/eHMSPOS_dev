-- =============================================
-- Hotel Management System - Room Management Tables
-- Created: 2025-11-19
-- Description: Room Master and Rate Master tables
-- =============================================

USE HotelApp;
GO

-- =============================================
-- Table: RoomTypes
-- Description: Master list of room types
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoomTypes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RoomTypes] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [TypeName] NVARCHAR(100) NOT NULL UNIQUE,
        [Description] NVARCHAR(500) NULL,
        [BaseRate] DECIMAL(10,2) NOT NULL DEFAULT 0,
        [MaxOccupancy] INT NOT NULL DEFAULT 2,
        [Amenities] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [LastModifiedDate] DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    
    CREATE INDEX IX_RoomTypes_IsActive ON [dbo].[RoomTypes]([IsActive]);
END
GO

-- =============================================
-- Table: Rooms
-- Description: Individual room inventory
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Rooms]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Rooms] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [RoomNumber] NVARCHAR(20) NOT NULL UNIQUE,
        [RoomTypeId] INT NOT NULL,
        [Floor] INT NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Available',
        [Notes] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [LastModifiedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_Rooms_RoomTypes FOREIGN KEY ([RoomTypeId]) REFERENCES [dbo].[RoomTypes]([Id])
    );
    
    CREATE INDEX IX_Rooms_RoomNumber ON [dbo].[Rooms]([RoomNumber]);
    CREATE INDEX IX_Rooms_Status ON [dbo].[Rooms]([Status]);
    CREATE INDEX IX_Rooms_Floor ON [dbo].[Rooms]([Floor]);
END
GO

-- =============================================
-- Table: RateTypes
-- Description: Customer and source classifications
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RateTypes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RateTypes] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [CustomerType] NVARCHAR(50) NOT NULL,
        [Source] NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        
        CONSTRAINT UQ_RateTypes_CustomerSource UNIQUE ([CustomerType], [Source])
    );
    
    CREATE INDEX IX_RateTypes_IsActive ON [dbo].[RateTypes]([IsActive]);
END
GO

-- =============================================
-- Table: RateMaster
-- Description: Dynamic pricing configuration
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RateMaster]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RateMaster] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [RoomTypeId] INT NOT NULL,
        [CustomerType] NVARCHAR(50) NOT NULL,
        [Source] NVARCHAR(50) NOT NULL,
        [BaseRate] DECIMAL(10,2) NOT NULL,
        [ExtraPaxRate] DECIMAL(10,2) NOT NULL DEFAULT 0,
        [TaxPercentage] DECIMAL(5,2) NOT NULL DEFAULT 0,
        [StartDate] DATE NOT NULL,
        [EndDate] DATE NOT NULL,
        [IsWeekdayRate] BIT NOT NULL DEFAULT 1,
        [ApplyDiscount] NVARCHAR(50) NULL,
        [IsDynamicRate] BIT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [CreatedBy] INT NULL,
        [LastModifiedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_RateMaster_RoomTypes FOREIGN KEY ([RoomTypeId]) REFERENCES [dbo].[RoomTypes]([Id]),
        CONSTRAINT FK_RateMaster_CreatedBy FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([Id])
    );
    
    CREATE INDEX IX_RateMaster_RoomType ON [dbo].[RateMaster]([RoomTypeId]);
    CREATE INDEX IX_RateMaster_Dates ON [dbo].[RateMaster]([StartDate], [EndDate]);
    CREATE INDEX IX_RateMaster_IsActive ON [dbo].[RateMaster]([IsActive]);
END
GO

PRINT 'Room management tables created successfully';
GO
