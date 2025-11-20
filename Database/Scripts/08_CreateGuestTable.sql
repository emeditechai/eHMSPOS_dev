-- =============================================
-- Hotel Management System - Guest Table
-- Created: 2025-11-20
-- Description: Guest records for lookup and history
-- =============================================

USE HMS_dev;
GO

-- =============================================
-- Table: Guests
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Guests]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Guests] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(50) NOT NULL,
        [Address] NVARCHAR(500) NULL,
        [City] NVARCHAR(100) NULL,
        [State] NVARCHAR(100) NULL,
        [Country] NVARCHAR(100) NULL,
        [IdentityType] NVARCHAR(50) NULL,
        [IdentityNumber] NVARCHAR(100) NULL,
        [DateOfBirth] DATE NULL,
        [LoyaltyId] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [LastModifiedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),

        CONSTRAINT UQ_Guests_Phone UNIQUE ([Phone])
    );

    CREATE INDEX IX_Guests_Phone ON [dbo].[Guests]([Phone]);
    CREATE INDEX IX_Guests_Email ON [dbo].[Guests]([Email]);
    CREATE INDEX IX_Guests_LoyaltyId ON [dbo].[Guests]([LoyaltyId]);
END
GO

PRINT 'Guest table created successfully';
GO
