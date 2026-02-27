-- =============================================
-- Booking cancellation & refund register
-- Created: 2026-02-08
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BookingCancellations')
BEGIN
    CREATE TABLE dbo.BookingCancellations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BookingId INT NOT NULL,
        BookingNumber NVARCHAR(25) NOT NULL,
        BranchID INT NOT NULL,

        Channel NVARCHAR(50) NULL,
        Source NVARCHAR(50) NULL,
        CustomerType NVARCHAR(50) NULL,
        RateType NVARCHAR(20) NULL,

        CancellationType NVARCHAR(30) NOT NULL,          -- Guest / Staff / AutoNoShow
        Reason NVARCHAR(500) NULL,
        IsOverride BIT NOT NULL CONSTRAINT DF_BookingCancellations_IsOverride DEFAULT (0),
        OverrideReason NVARCHAR(500) NULL,

        CancelRequestedBy INT NULL,
        CancelRequestedAt DATETIME2 NOT NULL CONSTRAINT DF_BookingCancellations_CancelRequestedAt DEFAULT (SYSUTCDATETIME()),

        CheckInAt DATETIME2 NULL,
        HoursBeforeCheckIn INT NOT NULL CONSTRAINT DF_BookingCancellations_HoursBeforeCheckIn DEFAULT (0),

        AmountPaid DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingCancellations_AmountPaid DEFAULT (0),
        RefundPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_BookingCancellations_RefundPercent DEFAULT (0),
        FlatDeduction DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingCancellations_FlatDeduction DEFAULT (0),
        GatewayFeeDeductionPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_BookingCancellations_GatewayFeeDeductionPercent DEFAULT (0),
        DeductionAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingCancellations_DeductionAmount DEFAULT (0),
        RefundAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingCancellations_RefundAmount DEFAULT (0),

        ApprovalStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_BookingCancellations_ApprovalStatus DEFAULT ('None'),  -- None/Pending/Approved/Rejected
        ApprovedBy INT NULL,
        ApprovedAt DATETIME2 NULL,

        PolicyId INT NULL,
        PolicySnapshot NVARCHAR(MAX) NULL,

        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_BookingCancellations_CreatedDate DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_BookingCancellations_Bookings FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
        CONSTRAINT FK_BookingCancellations_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID)
    );

    CREATE INDEX IX_BookingCancellations_BookingId ON dbo.BookingCancellations(BookingId);
    CREATE INDEX IX_BookingCancellations_BranchDate ON dbo.BookingCancellations(BranchID, CancelRequestedAt);
    CREATE INDEX IX_BookingCancellations_ApprovalStatus ON dbo.BookingCancellations(BranchID, ApprovalStatus);

    PRINT 'Table BookingCancellations created successfully';
END
ELSE
BEGIN
    PRINT 'Table BookingCancellations already exists';
END
GO
