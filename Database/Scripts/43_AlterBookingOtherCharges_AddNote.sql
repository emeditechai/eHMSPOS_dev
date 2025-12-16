SET NOCOUNT ON;

-- Adds Note column for BookingOtherCharges (upgrade script)
IF OBJECT_ID('dbo.BookingOtherCharges', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.BookingOtherCharges', 'Note') IS NULL
    BEGIN
        ALTER TABLE dbo.BookingOtherCharges
            ADD Note NVARCHAR(500) NULL;

        PRINT 'Added dbo.BookingOtherCharges.Note.';
    END
    ELSE
    BEGIN
        PRINT 'dbo.BookingOtherCharges.Note already exists; skipping.';
    END

    -- Ensure index include list covers Note (best-effort)
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
                INCLUDE (OtherChargeId, Qty, Rate, Note, GSTAmount, CGSTAmount, SGSTAmount);

            PRINT 'Recreated index IX_BookingOtherCharges_BookingId with Note included.';
        END
    END
END
ELSE
BEGIN
    PRINT 'dbo.BookingOtherCharges does not exist; apply 41_CreateBookingOtherCharges.sql first.';
END
