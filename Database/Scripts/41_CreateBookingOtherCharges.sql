SET NOCOUNT ON;

IF OBJECT_ID('dbo.BookingOtherCharges', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BookingOtherCharges
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BookingOtherCharges PRIMARY KEY,
        BookingId INT NOT NULL,
        OtherChargeId INT NOT NULL,
        Qty INT NOT NULL CONSTRAINT DF_BookingOtherCharges_Qty DEFAULT(1),
        Rate DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingOtherCharges_Rate DEFAULT(0),
        GSTAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingOtherCharges_GSTAmount DEFAULT(0),
        CGSTAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingOtherCharges_CGSTAmount DEFAULT(0),
        SGSTAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingOtherCharges_SGSTAmount DEFAULT(0),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_BookingOtherCharges_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,

        CONSTRAINT FK_BookingOtherCharges_Bookings FOREIGN KEY (BookingId)
            REFERENCES dbo.Bookings(Id) ON DELETE CASCADE,

        CONSTRAINT FK_BookingOtherCharges_OtherCharges FOREIGN KEY (OtherChargeId)
            REFERENCES dbo.OtherCharges(Id)
    );

    ALTER TABLE dbo.BookingOtherCharges
        ADD CONSTRAINT UQ_BookingOtherCharges_BookingId_OtherChargeId UNIQUE (BookingId, OtherChargeId);

    CREATE INDEX IX_BookingOtherCharges_BookingId
        ON dbo.BookingOtherCharges (BookingId)
        INCLUDE (OtherChargeId, Qty, Rate, GSTAmount, CGSTAmount, SGSTAmount);
END
ELSE
BEGIN
    PRINT 'dbo.BookingOtherCharges already exists; skipping create.';

    IF COL_LENGTH('dbo.BookingOtherCharges', 'Qty') IS NULL
    BEGIN
        ALTER TABLE dbo.BookingOtherCharges
            ADD Qty INT NOT NULL CONSTRAINT DF_BookingOtherCharges_Qty DEFAULT(1);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'UQ_BookingOtherCharges_BookingId_OtherChargeId'
          AND object_id = OBJECT_ID('dbo.BookingOtherCharges')
    )
    BEGIN
        BEGIN TRY
            ALTER TABLE dbo.BookingOtherCharges
                ADD CONSTRAINT UQ_BookingOtherCharges_BookingId_OtherChargeId UNIQUE (BookingId, OtherChargeId);
        END TRY
        BEGIN CATCH
            PRINT 'Could not add unique constraint UQ_BookingOtherCharges_BookingId_OtherChargeId.';
        END CATCH
    END

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
    END
END
