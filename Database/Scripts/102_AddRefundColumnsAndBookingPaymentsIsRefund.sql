-- =============================================
-- Refund tracking columns
-- Created: 2026-02-27
-- =============================================

USE HMS_dev;
GO

-- -----------------------------------------------
-- 1. BookingPayments: IsRefund flag
-- -----------------------------------------------
IF COL_LENGTH('dbo.BookingPayments', 'IsRefund') IS NULL
BEGIN
    ALTER TABLE dbo.BookingPayments
        ADD IsRefund BIT NOT NULL CONSTRAINT DF_BookingPayments_IsRefund DEFAULT (0);
    PRINT 'Column IsRefund added to BookingPayments';
END
ELSE
    PRINT 'Column IsRefund already exists in BookingPayments';
GO

-- -----------------------------------------------
-- 2. BookingCancellations: IsRefunded + audit cols
-- -----------------------------------------------
IF COL_LENGTH('dbo.BookingCancellations', 'IsRefunded') IS NULL
BEGIN
    ALTER TABLE dbo.BookingCancellations
        ADD IsRefunded BIT NOT NULL CONSTRAINT DF_BookingCancellations_IsRefunded DEFAULT (0);
    PRINT 'Column IsRefunded added to BookingCancellations';
END
ELSE
    PRINT 'Column IsRefunded already exists in BookingCancellations';
GO

IF COL_LENGTH('dbo.BookingCancellations', 'RefundedAt') IS NULL
BEGIN
    ALTER TABLE dbo.BookingCancellations
        ADD RefundedAt DATETIME2 NULL;
    PRINT 'Column RefundedAt added to BookingCancellations';
END
GO

IF COL_LENGTH('dbo.BookingCancellations', 'RefundedBy') IS NULL
BEGIN
    ALTER TABLE dbo.BookingCancellations
        ADD RefundedBy INT NULL;
    PRINT 'Column RefundedBy added to BookingCancellations';
END
GO

IF COL_LENGTH('dbo.BookingCancellations', 'RefundPaymentId') IS NULL
BEGIN
    ALTER TABLE dbo.BookingCancellations
        ADD RefundPaymentId INT NULL;
    PRINT 'Column RefundPaymentId added to BookingCancellations';
END
GO

IF COL_LENGTH('dbo.BookingCancellations', 'RefundPaymentMethod') IS NULL
BEGIN
    ALTER TABLE dbo.BookingCancellations
        ADD RefundPaymentMethod NVARCHAR(50) NULL;
    PRINT 'Column RefundPaymentMethod added to BookingCancellations';
END
GO

IF COL_LENGTH('dbo.BookingCancellations', 'RefundReference') IS NULL
BEGIN
    ALTER TABLE dbo.BookingCancellations
        ADD RefundReference NVARCHAR(200) NULL;
    PRINT 'Column RefundReference added to BookingCancellations';
END
GO

IF COL_LENGTH('dbo.BookingCancellations', 'RefundRemarks') IS NULL
BEGIN
    ALTER TABLE dbo.BookingCancellations
        ADD RefundRemarks NVARCHAR(500) NULL;
    PRINT 'Column RefundRemarks added to BookingCancellations';
END
GO

PRINT 'Script 102 complete.';
GO
