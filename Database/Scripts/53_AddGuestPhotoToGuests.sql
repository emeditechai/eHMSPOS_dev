-- =============================================
-- Hotel Management System - Add Guest Photo Columns
-- Created: 2025-12-27
-- Description: Stores captured webcam photo for primary guest
-- =============================================

USE HMS_dev;
GO

IF COL_LENGTH('dbo.Guests', 'Photo') IS NULL
BEGIN
    ALTER TABLE [dbo].[Guests] ADD [Photo] VARBINARY(MAX) NULL;
    PRINT 'Added Guests.Photo';
END
ELSE
BEGIN
    PRINT 'Guests.Photo already exists';
END
GO

IF COL_LENGTH('dbo.Guests', 'PhotoContentType') IS NULL
BEGIN
    ALTER TABLE [dbo].[Guests] ADD [PhotoContentType] NVARCHAR(100) NULL;
    PRINT 'Added Guests.PhotoContentType';
END
ELSE
BEGIN
    PRINT 'Guests.PhotoContentType already exists';
END
GO
