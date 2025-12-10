-- Migration: Alter SpecialDayRates to support date ranges
-- Description: Changes SpecialDate to FromDate and adds ToDate for date range support

-- Add new columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SpecialDayRates') AND name = 'FromDate')
BEGIN
    ALTER TABLE SpecialDayRates
    ADD FromDate DATE NULL;
    
    PRINT 'Added FromDate column to SpecialDayRates table.';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SpecialDayRates') AND name = 'ToDate')
BEGIN
    ALTER TABLE SpecialDayRates
    ADD ToDate DATE NULL;
    
    PRINT 'Added ToDate column to SpecialDayRates table.';
END
GO

-- Migrate existing data: Copy SpecialDate to FromDate and ToDate
UPDATE SpecialDayRates
SET FromDate = SpecialDate,
    ToDate = SpecialDate
WHERE FromDate IS NULL;
GO

-- Make the new columns NOT NULL after data migration
ALTER TABLE SpecialDayRates
ALTER COLUMN FromDate DATE NOT NULL;

ALTER TABLE SpecialDayRates
ALTER COLUMN ToDate DATE NOT NULL;
GO

-- Drop the old SpecialDate column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SpecialDayRates') AND name = 'SpecialDate')
BEGIN
    ALTER TABLE SpecialDayRates
    DROP COLUMN SpecialDate;
    
    PRINT 'Removed SpecialDate column from SpecialDayRates table.';
END
GO

-- Update the index to use FromDate instead of SpecialDate
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SpecialDayRates_SpecialDate' AND object_id = OBJECT_ID(N'SpecialDayRates'))
BEGIN
    DROP INDEX IX_SpecialDayRates_SpecialDate ON SpecialDayRates;
    PRINT 'Dropped old index IX_SpecialDayRates_SpecialDate.';
END
GO

-- Create new indexes for date range queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SpecialDayRates_FromDate' AND object_id = OBJECT_ID(N'SpecialDayRates'))
BEGIN
    CREATE INDEX IX_SpecialDayRates_FromDate ON SpecialDayRates(FromDate);
    PRINT 'Created index IX_SpecialDayRates_FromDate.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SpecialDayRates_ToDate' AND object_id = OBJECT_ID(N'SpecialDayRates'))
BEGIN
    CREATE INDEX IX_SpecialDayRates_ToDate ON SpecialDayRates(ToDate);
    PRINT 'Created index IX_SpecialDayRates_ToDate.';
END
GO

-- Add a check constraint to ensure FromDate <= ToDate
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CHK_SpecialDayRates_DateRange')
BEGIN
    ALTER TABLE SpecialDayRates
    ADD CONSTRAINT CHK_SpecialDayRates_DateRange CHECK (FromDate <= ToDate);
    PRINT 'Added check constraint CHK_SpecialDayRates_DateRange.';
END
GO

PRINT 'Migration completed: SpecialDayRates now supports date ranges.';
