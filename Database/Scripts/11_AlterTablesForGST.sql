-- =============================================
-- Hotel Management System - GST Column Updates
-- Created: 2025-11-20
-- Description: Update tables to use GST (CGST/SGST) instead of generic Tax
-- =============================================

USE HMS_dev;
GO

-- =============================================
-- Update Bookings Table - Add CGST/SGST columns
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'CGSTAmount')
BEGIN
    ALTER TABLE [dbo].[Bookings]
    ADD [CGSTAmount] DECIMAL(12,2) NULL;
    
    PRINT 'Added CGSTAmount column to Bookings table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'SGSTAmount')
BEGIN
    ALTER TABLE [dbo].[Bookings]
    ADD [SGSTAmount] DECIMAL(12,2) NULL;
    
    PRINT 'Added SGSTAmount column to Bookings table';
END
GO

-- Update existing records to split TaxAmount into CGST/SGST
UPDATE [dbo].[Bookings]
SET [CGSTAmount] = [TaxAmount] / 2,
    [SGSTAmount] = [TaxAmount] / 2
WHERE [CGSTAmount] IS NULL OR [SGSTAmount] IS NULL;
GO

-- Make columns NOT NULL after populating
ALTER TABLE [dbo].[Bookings]
ALTER COLUMN [CGSTAmount] DECIMAL(12,2) NOT NULL;
GO

ALTER TABLE [dbo].[Bookings]
ALTER COLUMN [SGSTAmount] DECIMAL(12,2) NOT NULL;
GO

-- =============================================
-- Update BookingRoomNights Table - Add CGST/SGST columns
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingRoomNights]') AND name = 'CGSTAmount')
BEGIN
    ALTER TABLE [dbo].[BookingRoomNights]
    ADD [CGSTAmount] DECIMAL(12,2) NULL;
    
    PRINT 'Added CGSTAmount column to BookingRoomNights table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingRoomNights]') AND name = 'SGSTAmount')
BEGIN
    ALTER TABLE [dbo].[BookingRoomNights]
    ADD [SGSTAmount] DECIMAL(12,2) NULL;
    
    PRINT 'Added SGSTAmount column to BookingRoomNights table';
END
GO

-- Update existing records to split TaxAmount into CGST/SGST
UPDATE [dbo].[BookingRoomNights]
SET [CGSTAmount] = [TaxAmount] / 2,
    [SGSTAmount] = [TaxAmount] / 2
WHERE [CGSTAmount] IS NULL OR [SGSTAmount] IS NULL;
GO

-- Make columns NOT NULL after populating
ALTER TABLE [dbo].[BookingRoomNights]
ALTER COLUMN [CGSTAmount] DECIMAL(12,2) NOT NULL;
GO

ALTER TABLE [dbo].[BookingRoomNights]
ALTER COLUMN [SGSTAmount] DECIMAL(12,2) NOT NULL;
GO

-- =============================================
-- Update RateMaster Table - Add GST fields
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RateMaster]') AND name = 'CGSTPercentage')
BEGIN
    ALTER TABLE [dbo].[RateMaster]
    ADD [CGSTPercentage] DECIMAL(5,2) NULL;
    
    PRINT 'Added CGSTPercentage column to RateMaster table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RateMaster]') AND name = 'SGSTPercentage')
BEGIN
    ALTER TABLE [dbo].[RateMaster]
    ADD [SGSTPercentage] DECIMAL(5,2) NULL;
    
    PRINT 'Added SGSTPercentage column to RateMaster table';
END
GO

-- Update existing records to split TaxPercentage into CGST/SGST
UPDATE [dbo].[RateMaster]
SET [CGSTPercentage] = [TaxPercentage] / 2,
    [SGSTPercentage] = [TaxPercentage] / 2
WHERE [CGSTPercentage] IS NULL OR [SGSTPercentage] IS NULL;
GO

-- Make columns NOT NULL after populating
ALTER TABLE [dbo].[RateMaster]
ALTER COLUMN [CGSTPercentage] DECIMAL(5,2) NOT NULL;
GO

ALTER TABLE [dbo].[RateMaster]
ALTER COLUMN [SGSTPercentage] DECIMAL(5,2) NOT NULL;
GO

PRINT 'GST column updates completed successfully';
GO
