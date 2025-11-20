-- =============================================
-- Hotel Management System - Guest Parent-Child Relationship
-- Created: 2025-11-20
-- Description: Add parent-child relationship support for family guests
-- =============================================

USE HMS_dev;
GO

-- =============================================
-- Add ParentGuestId column for parent-child relationship
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Guests]') AND name = 'ParentGuestId')
BEGIN
    ALTER TABLE [dbo].[Guests]
    ADD [ParentGuestId] INT NULL;
    
    -- Add foreign key to self-reference
    ALTER TABLE [dbo].[Guests]
    ADD CONSTRAINT FK_Guests_ParentGuest 
    FOREIGN KEY ([ParentGuestId]) REFERENCES [dbo].[Guests]([Id]);
    
    PRINT 'Added ParentGuestId column with self-referencing FK to Guests table';
END
GO

-- =============================================
-- Add GuestType column to distinguish primary/companion/child
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Guests]') AND name = 'GuestType')
BEGIN
    ALTER TABLE [dbo].[Guests]
    ADD [GuestType] NVARCHAR(50) NULL;
    
    -- Update existing records
    UPDATE [dbo].[Guests]
    SET [GuestType] = 'Primary'
    WHERE [GuestType] IS NULL;
    
    -- Make it NOT NULL after populating
    ALTER TABLE [dbo].[Guests]
    ALTER COLUMN [GuestType] NVARCHAR(50) NOT NULL;
    
    PRINT 'Added GuestType column to Guests table';
END
GO

-- =============================================
-- Create index for faster lookups
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Guests_Phone' AND object_id = OBJECT_ID(N'[dbo].[Guests]'))
BEGIN
    CREATE INDEX IX_Guests_Phone ON [dbo].[Guests]([Phone]);
    PRINT 'Created index on Phone column';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Guests_Email' AND object_id = OBJECT_ID(N'[dbo].[Guests]'))
BEGIN
    CREATE INDEX IX_Guests_Email ON [dbo].[Guests]([Email]);
    PRINT 'Created index on Email column';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Guests_ParentGuestId' AND object_id = OBJECT_ID(N'[dbo].[Guests]'))
BEGIN
    CREATE INDEX IX_Guests_ParentGuestId ON [dbo].[Guests]([ParentGuestId]);
    PRINT 'Created index on ParentGuestId column';
END
GO

PRINT 'Guest parent-child relationship setup completed successfully';
GO
