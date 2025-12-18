-- Add IsAdvancePayment flag to BookingPayments table
-- This column tracks whether a payment is an advance payment made during booking creation

IF COL_LENGTH('dbo.BookingPayments', 'IsAdvancePayment') IS NULL
BEGIN
    ALTER TABLE dbo.BookingPayments
    ADD IsAdvancePayment BIT NOT NULL CONSTRAINT DF_BookingPayments_IsAdvancePayment DEFAULT (0);

    PRINT 'Column IsAdvancePayment added to BookingPayments';
END
ELSE
BEGIN
    PRINT 'Column IsAdvancePayment already exists in BookingPayments';
END
GO

-- Recreate stored procedure to insert booking payment (include IsAdvancePayment)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_InsertBookingPayment]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_InsertBookingPayment];
GO

CREATE PROCEDURE [dbo].[sp_InsertBookingPayment]
    @BookingID INT,
    @PaymentDate DATETIME,
    @Amount DECIMAL(18,2),
    @PaymentMode NVARCHAR(50),
    @TransactionID NVARCHAR(100) = NULL,
    @Remarks NVARCHAR(500) = NULL,
    @BranchID INT,
    @CreatedBy INT,
    @ReceiptNumber NVARCHAR(50) = NULL,
    @IsAdvancePayment BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO BookingPayments (
        BookingID,
        PaymentDate,
        Amount,
        PaymentMode,
        TransactionID,
        Remarks,
        BranchID,
        CreatedDate,
        CreatedBy,
        ReceiptNumber,
        IsAdvancePayment
    )
    VALUES (
        @BookingID,
        @PaymentDate,
        @Amount,
        @PaymentMode,
        @TransactionID,
        @Remarks,
        @BranchID,
        GETDATE(),
        @CreatedBy,
        @ReceiptNumber,
        @IsAdvancePayment
    );

    SELECT SCOPE_IDENTITY() AS PaymentID;
END
GO

PRINT 'Stored procedure sp_InsertBookingPayment updated to include IsAdvancePayment';
GO
