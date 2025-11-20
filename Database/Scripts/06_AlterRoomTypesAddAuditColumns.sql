-- Add CreatedBy and LastModifiedBy columns to RoomTypes table

-- Check if CreatedBy column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RoomTypes]') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE [dbo].[RoomTypes]
    ADD [CreatedBy] INT NULL;
    PRINT 'CreatedBy column added to RoomTypes table';
END
ELSE
BEGIN
    PRINT 'CreatedBy column already exists in RoomTypes table';
END

-- Check if LastModifiedBy column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RoomTypes]') AND name = 'LastModifiedBy')
BEGIN
    ALTER TABLE [dbo].[RoomTypes]
    ADD [LastModifiedBy] INT NULL;
    PRINT 'LastModifiedBy column added to RoomTypes table';
END
ELSE
BEGIN
    PRINT 'LastModifiedBy column already exists in RoomTypes table';
END

PRINT 'RoomTypes table audit columns update completed';
