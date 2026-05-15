-- ============================================================
-- Script 156: Banquet Booking Number Counter (separate from Receipt Counter)
-- Fixes counter conflict: booking numbers and receipt numbers
-- now each have their own independent sequence per branch per year.
-- ============================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.BanquetBookingCounter', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetBookingCounter
    (
        BranchID    INT NOT NULL CONSTRAINT PK_BanquetBookingCounter PRIMARY KEY,
        [Year]      INT NOT NULL CONSTRAINT DF_BanquetBookingCounter_Year DEFAULT (YEAR(GETDATE())),
        LastNumber  INT NOT NULL CONSTRAINT DF_BanquetBookingCounter_LastNumber DEFAULT (0)
    );

    -- Initialize a row for every existing branch
    INSERT INTO dbo.BanquetBookingCounter (BranchID, [Year], LastNumber)
    SELECT BranchID, YEAR(GETDATE()), 0
    FROM dbo.BranchMaster
    WHERE BranchID NOT IN (SELECT BranchID FROM dbo.BanquetBookingCounter);

    PRINT 'Created dbo.BanquetBookingCounter';
END
ELSE
    PRINT 'dbo.BanquetBookingCounter already exists; skipping.';
GO

PRINT 'Script 156 completed.';
GO
