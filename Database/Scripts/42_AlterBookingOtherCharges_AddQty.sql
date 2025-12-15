SET NOCOUNT ON;

-- Adds Qty column for BookingOtherCharges (upgrade script)
IF OBJECT_ID('dbo.BookingOtherCharges', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.BookingOtherCharges', 'Qty') IS NULL
    BEGIN
        ALTER TABLE dbo.BookingOtherCharges
            ADD Qty INT NOT NULL CONSTRAINT DF_BookingOtherCharges_Qty DEFAULT(1);

        PRINT 'Added dbo.BookingOtherCharges.Qty.';
    END
    ELSE
    BEGIN
        PRINT 'dbo.BookingOtherCharges.Qty already exists; skipping.';
    END

    -- Ensure index include list covers Qty (best-effort)
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_BookingOtherCharges_BookingId'
          AND object_id = OBJECT_ID('dbo.BookingOtherCharges')
    )
    BEGIN
        -- Drop and recreate to ensure INCLUDE columns match
        BEGIN TRY
            DROP INDEX IX_BookingOtherCharges_BookingId ON dbo.BookingOtherCharges;
        END TRY
        BEGIN CATCH
            PRINT 'Could not drop index IX_BookingOtherCharges_BookingId (it may be in use).';
        END CATCH

        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_BookingOtherCharges_BookingId'
              AND object_id = OBJECT_ID('dbo.BookingOtherCharges')
        )
        BEGIN
            CREATE INDEX IX_BookingOtherCharges_BookingId
                ON dbo.BookingOtherCharges (BookingId)
                INCLUDE (OtherChargeId, Qty, Rate, GSTAmount, CGSTAmount, SGSTAmount);

            PRINT 'Recreated index IX_BookingOtherCharges_BookingId with Qty included.';
        END
    END
END
ELSE
BEGIN
    PRINT 'dbo.BookingOtherCharges does not exist; apply 41_CreateBookingOtherCharges.sql first.';
END
