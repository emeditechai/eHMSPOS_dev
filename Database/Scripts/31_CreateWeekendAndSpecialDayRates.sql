-- Migration: Create Weekend Rate and Special Day Rate Tables
-- Description: Adds support for weekend-specific and special day-specific rate configurations

-- Create WeekendRates table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WeekendRates' AND type = 'U')
BEGIN
    CREATE TABLE WeekendRates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RateMasterId INT NOT NULL,
        DayOfWeek NVARCHAR(20) NOT NULL, -- 'Saturday', 'Sunday', 'Friday', etc.
        BaseRate DECIMAL(18,2) NOT NULL,
        ExtraPaxRate DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
        LastModifiedBy INT NULL,
        CONSTRAINT FK_WeekendRates_RateMaster FOREIGN KEY (RateMasterId) REFERENCES RateMaster(Id) ON DELETE CASCADE,
        CONSTRAINT CHK_WeekendRates_DayOfWeek CHECK (DayOfWeek IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'))
    );
    
    CREATE INDEX IX_WeekendRates_RateMasterId ON WeekendRates(RateMasterId);
    CREATE INDEX IX_WeekendRates_DayOfWeek ON WeekendRates(DayOfWeek);
    
    PRINT 'WeekendRates table created successfully.';
END
ELSE
BEGIN
    PRINT 'WeekendRates table already exists.';
END
GO

-- Create SpecialDayRates table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SpecialDayRates' AND type = 'U')
BEGIN
    CREATE TABLE SpecialDayRates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RateMasterId INT NOT NULL,
        SpecialDate DATE NOT NULL,
        EventName NVARCHAR(100) NULL, -- e.g., 'New Year', 'Christmas', 'Festival', etc.
        BaseRate DECIMAL(18,2) NOT NULL,
        ExtraPaxRate DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
        LastModifiedBy INT NULL,
        CONSTRAINT FK_SpecialDayRates_RateMaster FOREIGN KEY (RateMasterId) REFERENCES RateMaster(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_SpecialDayRates_RateMasterId ON SpecialDayRates(RateMasterId);
    CREATE INDEX IX_SpecialDayRates_SpecialDate ON SpecialDayRates(SpecialDate);
    
    PRINT 'SpecialDayRates table created successfully.';
END
ELSE
BEGIN
    PRINT 'SpecialDayRates table already exists.';
END
GO

-- Add comments describing the tables
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Stores weekend-specific rates for different days of the week',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'WeekendRates';
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Stores special day rates for holidays, events, or specific dates',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'SpecialDayRates';
GO

PRINT 'Migration completed successfully.';
