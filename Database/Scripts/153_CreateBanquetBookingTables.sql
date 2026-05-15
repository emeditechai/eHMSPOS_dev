-- ============================================================
-- Script 153: Banquet Booking Core Tables
-- BanquetBookings header, PackageLines, AddonLines,
-- Payments, AuditLog, Cancellations
-- ============================================================
SET NOCOUNT ON;
GO

-- --------------------------------------------------------
-- 1. BanquetBookings (Main Header)
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetBookings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetBookings
    (
        Id                       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetBookings PRIMARY KEY,
        BanquetBookingNumber     NVARCHAR(30)     NOT NULL,
        BranchID                 INT              NOT NULL,

        -- Event Date & Time
        EventDate                DATE             NOT NULL,
        EventEndDate             DATE             NULL,   -- for multi-day events
        EventStartTime           TIME             NULL,
        EventEndTime             TIME             NULL,
        SetupTime                TIME             NULL,
        TeardownTime             TIME             NULL,

        -- Venue
        VenueId                  INT              NOT NULL,

        -- Event Details
        EventTypeId              INT              NOT NULL,
        EventName                NVARCHAR(200)    NOT NULL,
        AttendeeCount            INT              NOT NULL CONSTRAINT DF_BanquetBookings_AttendeeCount DEFAULT (0),
        GuaranteePax             INT              NOT NULL CONSTRAINT DF_BanquetBookings_GuaranteePax DEFAULT (0),
        ChildCount               INT              NOT NULL CONSTRAINT DF_BanquetBookings_ChildCount DEFAULT (0),
        MealType                 NVARCHAR(20)     NOT NULL CONSTRAINT DF_BanquetBookings_MealType DEFAULT ('Veg'),

        -- Customer (B2C / B2B)
        CustomerType             NVARCHAR(10)     NOT NULL CONSTRAINT DF_BanquetBookings_CustomerType DEFAULT ('B2C'),
        -- B2C
        PrimaryGuestId           INT              NULL,
        GuestName                NVARCHAR(200)    NOT NULL CONSTRAINT DF_BanquetBookings_GuestName DEFAULT (''),
        GuestPhone               NVARCHAR(20)     NOT NULL CONSTRAINT DF_BanquetBookings_GuestPhone DEFAULT (''),
        GuestEmail               NVARCHAR(150)    NULL,
        GuestAddress             NVARCHAR(500)    NULL,
        GuestGSTIN               NVARCHAR(15)     NULL,  -- B2C client with GST (optional)
        -- B2B
        B2BClientId              INT              NULL,
        B2BAgreementId           INT              NULL,
        CompanyName              NVARCHAR(150)    NULL,
        CompanyGSTIN             NVARCHAR(15)     NULL,
        CompanyPAN               NVARCHAR(10)     NULL,
        CompanyAddress           NVARCHAR(500)    NULL,
        BillingTo                NVARCHAR(10)     NOT NULL CONSTRAINT DF_BanquetBookings_BillingTo DEFAULT ('Guest'),
        CreditDays               INT              NOT NULL CONSTRAINT DF_BanquetBookings_CreditDays DEFAULT (0),
        IsInterState             BIT              NOT NULL CONSTRAINT DF_BanquetBookings_IsInterState DEFAULT (0),

        -- Package
        PackageId                INT              NULL,
        PackagePricePerPax       DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_PackagePricePerPax DEFAULT (0),
        PackageTotalPax          INT              NOT NULL CONSTRAINT DF_BanquetBookings_PackageTotalPax DEFAULT (0),
        PackageBaseAmount        DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_PackageBaseAmount DEFAULT (0),
        PackageGSTAmount         DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_PackageGSTAmount DEFAULT (0),
        PackageCGSTAmount        DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_PackageCGSTAmount DEFAULT (0),
        PackageSGSTAmount        DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_PackageSGSTAmount DEFAULT (0),
        PackageIGSTAmount        DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_PackageIGSTAmount DEFAULT (0),

        -- Venue Hire
        VenueHireType            NVARCHAR(10)     NOT NULL CONSTRAINT DF_BanquetBookings_VenueHireType DEFAULT ('FullDay'),
        VenueBaseAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_VenueBaseAmount DEFAULT (0),
        VenueGSTAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_VenueGSTAmount DEFAULT (0),
        VenueCGSTAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_VenueCGSTAmount DEFAULT (0),
        VenueSGSTAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_VenueSGSTAmount DEFAULT (0),
        VenueIGSTAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_VenueIGSTAmount DEFAULT (0),

        -- Addon Services total
        AddonBaseAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_AddonBaseAmount DEFAULT (0),
        AddonGSTAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_AddonGSTAmount DEFAULT (0),
        AddonCGSTAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_AddonCGSTAmount DEFAULT (0),
        AddonSGSTAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_AddonSGSTAmount DEFAULT (0),
        AddonIGSTAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_AddonIGSTAmount DEFAULT (0),

        -- Grand Totals
        TotalBaseAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_TotalBaseAmount DEFAULT (0),
        TotalGSTAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_TotalGSTAmount DEFAULT (0),
        TotalCGSTAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_TotalCGSTAmount DEFAULT (0),
        TotalSGSTAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_TotalSGSTAmount DEFAULT (0),
        TotalIGSTAmount          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_TotalIGSTAmount DEFAULT (0),
        ServiceChargeAmount      DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_ServiceChargeAmount DEFAULT (0),
        DiscountAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_DiscountAmount DEFAULT (0),
        RoundOffAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_RoundOffAmount DEFAULT (0),
        TotalAmount              DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_TotalAmount DEFAULT (0),
        DepositAmount            DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_DepositAmount DEFAULT (0),
        BalanceAmount            DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BanquetBookings_BalanceAmount DEFAULT (0),

        -- Status
        [Status]                 NVARCHAR(20)     NOT NULL CONSTRAINT DF_BanquetBookings_Status DEFAULT ('Inquiry'),
        PaymentStatus            NVARCHAR(20)     NOT NULL CONSTRAINT DF_BanquetBookings_PaymentStatus DEFAULT ('Pending'),
        ApprovalStatus           NVARCHAR(20)     NOT NULL CONSTRAINT DF_BanquetBookings_ApprovalStatus DEFAULT ('Draft'),

        -- Cancellation
        CancellationPolicyId     INT              NULL,
        CancellationPolicySnapshot NVARCHAR(MAX)  NULL,

        -- Linked hotel booking (corporate events with room stays)
        LinkedHotelBookingId     INT              NULL,
        LinkedHotelBookingNumber NVARCHAR(30)     NULL,

        -- Invoice
        InvoiceNumber            NVARCHAR(50)     NULL,

        -- Misc
        SpecialRequests          NVARCHAR(2000)   NULL,
        InternalNotes            NVARCHAR(2000)   NULL,

        -- Audit
        CreatedDate              DATETIME2 NOT NULL CONSTRAINT DF_BanquetBookings_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy                INT NULL,
        LastModifiedDate         DATETIME2 NULL,
        LastModifiedBy           INT NULL,

        CONSTRAINT UQ_BanquetBookings_BookingNumber UNIQUE (BanquetBookingNumber),
        CONSTRAINT FK_BanquetBookings_Branch    FOREIGN KEY (BranchID)     REFERENCES dbo.BranchMaster(BranchID),
        CONSTRAINT FK_BanquetBookings_Venue     FOREIGN KEY (VenueId)      REFERENCES dbo.BanquetVenues(Id),
        CONSTRAINT FK_BanquetBookings_EventType FOREIGN KEY (EventTypeId)  REFERENCES dbo.BanquetEventTypes(Id),
        CONSTRAINT FK_BanquetBookings_Package   FOREIGN KEY (PackageId)    REFERENCES dbo.BanquetPackages(Id),
        CONSTRAINT FK_BanquetBookings_Guest     FOREIGN KEY (PrimaryGuestId) REFERENCES dbo.Guests(Id),
        CONSTRAINT FK_BanquetBookings_B2BClient FOREIGN KEY (B2BClientId)  REFERENCES dbo.B2BClients(Id),
        CONSTRAINT FK_BanquetBookings_B2BAgreement FOREIGN KEY (B2BAgreementId) REFERENCES dbo.B2BAgreements(Id),
        CONSTRAINT FK_BanquetBookings_CancellationPolicy FOREIGN KEY (CancellationPolicyId) REFERENCES dbo.CancellationPolicies(Id),
        CONSTRAINT FK_BanquetBookings_LinkedBooking FOREIGN KEY (LinkedHotelBookingId) REFERENCES dbo.Bookings(Id)
    );

    CREATE INDEX IX_BanquetBookings_Branch_Status    ON dbo.BanquetBookings(BranchID, [Status]);
    CREATE INDEX IX_BanquetBookings_EventDate         ON dbo.BanquetBookings(EventDate);
    CREATE INDEX IX_BanquetBookings_Venue_EventDate   ON dbo.BanquetBookings(VenueId, EventDate);
    CREATE INDEX IX_BanquetBookings_B2BClient         ON dbo.BanquetBookings(B2BClientId);
    PRINT 'Created dbo.BanquetBookings';
END
ELSE
    PRINT 'dbo.BanquetBookings already exists; skipping.';
GO

-- --------------------------------------------------------
-- 2. BanquetBookingPackageLines (snapshot of package at booking)
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetBookingPackageLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetBookingPackageLines
    (
        Id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetBookingPackageLines PRIMARY KEY,
        BanquetBookingId     INT              NOT NULL,
        PackageId            INT              NULL,
        PackageName          NVARCHAR(150)    NOT NULL,
        PackageType          NVARCHAR(30)     NOT NULL,
        MealType             NVARCHAR(20)     NOT NULL CONSTRAINT DF_BBPackageLines_MealType DEFAULT ('Veg'),
        PricePerPax          DECIMAL(18,2)    NOT NULL,
        Pax                  INT              NOT NULL CONSTRAINT DF_BBPackageLines_Pax DEFAULT (0),
        BaseAmount           DECIMAL(18,2)    NOT NULL,
        GSTPercent           DECIMAL(6,2)     NOT NULL CONSTRAINT DF_BBPackageLines_GSTPercent DEFAULT (0),
        CGSTPercent          DECIMAL(6,2)     NOT NULL CONSTRAINT DF_BBPackageLines_CGSTPercent DEFAULT (0),
        SGSTPercent          DECIMAL(6,2)     NOT NULL CONSTRAINT DF_BBPackageLines_SGSTPercent DEFAULT (0),
        IGSTPercent          DECIMAL(6,2)     NOT NULL CONSTRAINT DF_BBPackageLines_IGSTPercent DEFAULT (0),
        GSTAmount            DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBPackageLines_GSTAmount DEFAULT (0),
        CGSTAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBPackageLines_CGSTAmount DEFAULT (0),
        SGSTAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBPackageLines_SGSTAmount DEFAULT (0),
        IGSTAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBPackageLines_IGSTAmount DEFAULT (0),
        TotalAmount          DECIMAL(18,2)    NOT NULL,
        MenuDescription      NVARCHAR(MAX)    NULL,
        SACCode              NVARCHAR(10)     NULL,
        CreatedDate          DATETIME2 NOT NULL CONSTRAINT DF_BBPackageLines_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_BBPackageLines_Booking  FOREIGN KEY (BanquetBookingId) REFERENCES dbo.BanquetBookings(Id) ON DELETE CASCADE,
        CONSTRAINT FK_BBPackageLines_Package  FOREIGN KEY (PackageId)        REFERENCES dbo.BanquetPackages(Id)
    );
    CREATE INDEX IX_BBPackageLines_Booking ON dbo.BanquetBookingPackageLines(BanquetBookingId);
    PRINT 'Created dbo.BanquetBookingPackageLines';
END
ELSE
    PRINT 'dbo.BanquetBookingPackageLines already exists; skipping.';
GO

-- --------------------------------------------------------
-- 3. BanquetBookingAddonLines (snapshot of addon at booking)
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetBookingAddonLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetBookingAddonLines
    (
        Id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetBookingAddonLines PRIMARY KEY,
        BanquetBookingId     INT              NOT NULL,
        AddonServiceId       INT              NULL,
        ServiceName          NVARCHAR(150)    NOT NULL,
        ServiceType          NVARCHAR(30)     NOT NULL,
        Rate                 DECIMAL(18,2)    NOT NULL,
        RateType             NVARCHAR(20)     NOT NULL,
        Qty                  DECIMAL(10,2)    NOT NULL CONSTRAINT DF_BBAddonLines_Qty DEFAULT (1),
        BaseAmount           DECIMAL(18,2)    NOT NULL,
        GSTPercent           DECIMAL(6,2)     NOT NULL CONSTRAINT DF_BBAddonLines_GSTPercent DEFAULT (0),
        CGSTPercent          DECIMAL(6,2)     NOT NULL CONSTRAINT DF_BBAddonLines_CGSTPercent DEFAULT (0),
        SGSTPercent          DECIMAL(6,2)     NOT NULL CONSTRAINT DF_BBAddonLines_SGSTPercent DEFAULT (0),
        IGSTPercent          DECIMAL(6,2)     NOT NULL CONSTRAINT DF_BBAddonLines_IGSTPercent DEFAULT (0),
        GSTAmount            DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBAddonLines_GSTAmount DEFAULT (0),
        CGSTAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBAddonLines_CGSTAmount DEFAULT (0),
        SGSTAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBAddonLines_SGSTAmount DEFAULT (0),
        IGSTAmount           DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBAddonLines_IGSTAmount DEFAULT (0),
        TotalAmount          DECIMAL(18,2)    NOT NULL,
        Notes                NVARCHAR(500)    NULL,
        SACCode              NVARCHAR(10)     NULL,
        CreatedDate          DATETIME2 NOT NULL CONSTRAINT DF_BBAddonLines_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_BBAddonLines_Booking FOREIGN KEY (BanquetBookingId) REFERENCES dbo.BanquetBookings(Id) ON DELETE CASCADE,
        CONSTRAINT FK_BBAddonLines_Addon   FOREIGN KEY (AddonServiceId)   REFERENCES dbo.BanquetAddonServices(Id)
    );
    CREATE INDEX IX_BBAddonLines_Booking ON dbo.BanquetBookingAddonLines(BanquetBookingId);
    PRINT 'Created dbo.BanquetBookingAddonLines';
END
ELSE
    PRINT 'dbo.BanquetBookingAddonLines already exists; skipping.';
GO

-- --------------------------------------------------------
-- 4. BanquetBookingPayments
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetBookingPayments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetBookingPayments
    (
        Id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetBookingPayments PRIMARY KEY,
        BanquetBookingId     INT              NOT NULL,
        ReceiptNumber        NVARCHAR(50)     NOT NULL,
        Amount               DECIMAL(18,2)    NOT NULL,
        PaymentMethod        NVARCHAR(20)     NOT NULL,
        PaymentReference     NVARCHAR(200)    NULL,
        [Status]             NVARCHAR(20)     NOT NULL CONSTRAINT DF_BBPayments_Status DEFAULT ('Captured'),
        PaidOn               DATETIME2        NOT NULL CONSTRAINT DF_BBPayments_PaidOn DEFAULT (SYSUTCDATETIME()),
        BankId               INT              NULL,
        CardType             NVARCHAR(30)     NULL,
        CardLastFourDigits   NVARCHAR(4)      NULL,
        ChequeDate           DATE             NULL,
        IsAdvancePayment     BIT NOT NULL     CONSTRAINT DF_BBPayments_IsAdvancePayment DEFAULT (0),
        IsRefund             BIT NOT NULL     CONSTRAINT DF_BBPayments_IsRefund DEFAULT (0),
        DiscountAmount       DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBPayments_DiscountAmount DEFAULT (0),
        RoundOffAmount       DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBPayments_RoundOffAmount DEFAULT (0),
        Remarks              NVARCHAR(500)    NULL,
        CreatedDate          DATETIME2 NOT NULL CONSTRAINT DF_BBPayments_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy            INT NULL,
        CONSTRAINT FK_BBPayments_Booking FOREIGN KEY (BanquetBookingId) REFERENCES dbo.BanquetBookings(Id) ON DELETE CASCADE,
        CONSTRAINT FK_BBPayments_Bank    FOREIGN KEY (BankId) REFERENCES dbo.Banks(Id)
    );
    CREATE INDEX IX_BBPayments_Booking ON dbo.BanquetBookingPayments(BanquetBookingId);
    CREATE INDEX IX_BBPayments_PaidOn  ON dbo.BanquetBookingPayments(PaidOn);
    PRINT 'Created dbo.BanquetBookingPayments';
END
ELSE
    PRINT 'dbo.BanquetBookingPayments already exists; skipping.';
GO

-- --------------------------------------------------------
-- 5. BanquetBookingAuditLog
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetBookingAuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetBookingAuditLog
    (
        Id                    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetBookingAuditLog PRIMARY KEY,
        BanquetBookingId      INT              NOT NULL,
        BanquetBookingNumber  NVARCHAR(30)     NOT NULL,
        ActionType            NVARCHAR(50)     NOT NULL,
        ActionDescription     NVARCHAR(1000)   NOT NULL,
        OldValue              NVARCHAR(MAX)    NULL,
        NewValue              NVARCHAR(MAX)    NULL,
        PerformedBy           INT              NULL,
        PerformedAt           DATETIME2 NOT NULL CONSTRAINT DF_BBAuditLog_PerformedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_BBAuditLog_Booking FOREIGN KEY (BanquetBookingId) REFERENCES dbo.BanquetBookings(Id)
    );
    CREATE INDEX IX_BBAuditLog_Booking ON dbo.BanquetBookingAuditLog(BanquetBookingId);
    PRINT 'Created dbo.BanquetBookingAuditLog';
END
ELSE
    PRINT 'dbo.BanquetBookingAuditLog already exists; skipping.';
GO

-- --------------------------------------------------------
-- 6. BanquetCancellations
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetCancellations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetCancellations
    (
        Id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetCancellations PRIMARY KEY,
        BanquetBookingId     INT              NOT NULL,
        BanquetBookingNumber NVARCHAR(30)     NOT NULL,
        AmountPaid           DECIMAL(18,2)    NOT NULL,
        RefundPercent        DECIMAL(6,2)     NOT NULL CONSTRAINT DF_BBCancellations_RefundPercent DEFAULT (0),
        FlatDeduction        DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBCancellations_FlatDeduction DEFAULT (0),
        DeductionAmount      DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBCancellations_DeductionAmount DEFAULT (0),
        RefundAmount         DECIMAL(18,2)    NOT NULL CONSTRAINT DF_BBCancellations_RefundAmount DEFAULT (0),
        IsRefunded           BIT NOT NULL     CONSTRAINT DF_BBCancellations_IsRefunded DEFAULT (0),
        RefundPaymentMethod  NVARCHAR(20)     NULL,
        RefundReference      NVARCHAR(200)    NULL,
        RefundedOn           DATETIME2        NULL,
        ApprovalStatus       NVARCHAR(20)     NOT NULL CONSTRAINT DF_BBCancellations_ApprovalStatus DEFAULT ('Pending'),
        CancellationReason   NVARCHAR(1000)   NULL,
        CancelledOn          DATETIME2 NOT NULL CONSTRAINT DF_BBCancellations_CancelledOn DEFAULT (SYSUTCDATETIME()),
        CancelledBy          INT NULL,
        CONSTRAINT FK_BBCancellations_Booking FOREIGN KEY (BanquetBookingId) REFERENCES dbo.BanquetBookings(Id)
    );
    CREATE INDEX IX_BBCancellations_Booking ON dbo.BanquetCancellations(BanquetBookingId);
    PRINT 'Created dbo.BanquetCancellations';
END
ELSE
    PRINT 'dbo.BanquetCancellations already exists; skipping.';
GO

-- --------------------------------------------------------
-- 7. Sequence / helper: receipt number counter for banquet
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetReceiptCounter', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetReceiptCounter
    (
        BranchID     INT NOT NULL CONSTRAINT PK_BanquetReceiptCounter PRIMARY KEY,
        LastNumber   INT NOT NULL CONSTRAINT DF_BanquetReceiptCounter_LastNumber DEFAULT (0)
    );
    PRINT 'Created dbo.BanquetReceiptCounter';
END
GO

-- Initialize counter for each branch
INSERT INTO dbo.BanquetReceiptCounter (BranchID, LastNumber)
SELECT BranchID, 0 FROM dbo.BranchMaster
WHERE BranchID NOT IN (SELECT BranchID FROM dbo.BanquetReceiptCounter);
GO

PRINT 'Script 153 completed successfully.';
GO
