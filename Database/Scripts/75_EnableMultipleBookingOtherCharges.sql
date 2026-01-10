SET NOCOUNT ON;

PRINT 'Starting migration: allow multiple booking other charges per day.';

IF EXISTS (
    SELECT 1
    FROM sys.objects o
    WHERE o.type = 'UQ'
      AND o.name = 'UQ_BookingOtherCharges_BookingId_OtherChargeId'
)
BEGIN
    ALTER TABLE dbo.BookingOtherCharges
        DROP CONSTRAINT UQ_BookingOtherCharges_BookingId_OtherChargeId;
    PRINT '  - Dropped unique constraint UQ_BookingOtherCharges_BookingId_OtherChargeId';
END
ELSE
BEGIN
    PRINT '  - Unique constraint already absent; nothing to drop.';
END

IF COL_LENGTH('dbo.BookingOtherCharges', 'ChargeDate') IS NULL
BEGIN
    ALTER TABLE dbo.BookingOtherCharges
        ADD ChargeDate DATE NOT NULL CONSTRAINT DF_BookingOtherCharges_ChargeDate DEFAULT (CONVERT(date, SYSUTCDATETIME()));
    PRINT '  - Added ChargeDate column with default.';
END
ELSE
BEGIN
    PRINT '  - ChargeDate column already exists; skipping add.';
END

UPDATE dbo.BookingOtherCharges
        SET ChargeDate = ISNULL(ChargeDate, ISNULL(CONVERT(date, CreatedDate), CONVERT(date, SYSUTCDATETIME())))
    WHERE ChargeDate IS NULL;

IF EXISTS (
    SELECT 1 FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.BookingOtherCharges')
      AND name = 'DF_BookingOtherCharges_ChargeDate'
)
BEGIN
    PRINT '  - Verified default constraint DF_BookingOtherCharges_ChargeDate.';
END
ELSE IF COL_LENGTH('dbo.BookingOtherCharges', 'ChargeDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.BookingOtherCharges
        ADD CONSTRAINT DF_BookingOtherCharges_ChargeDate DEFAULT (CONVERT(date, SYSUTCDATETIME())) FOR ChargeDate;
    PRINT '  - Recreated default constraint for ChargeDate.';
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_BookingOtherCharges_BookingId_ChargeDate'
      AND object_id = OBJECT_ID('dbo.BookingOtherCharges')
)
BEGIN
    CREATE INDEX IX_BookingOtherCharges_BookingId_ChargeDate
        ON dbo.BookingOtherCharges (BookingId, ChargeDate, OtherChargeId);
    PRINT '  - Created index IX_BookingOtherCharges_BookingId_ChargeDate.';
END
ELSE
BEGIN
    PRINT '  - Index IX_BookingOtherCharges_BookingId_ChargeDate already present.';
END

PRINT 'Completed migration: allow multiple booking other charges per day.';
