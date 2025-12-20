-- Migration: Add Discount and RoundOff columns to BookingPayments table

IF COL_LENGTH('dbo.BookingPayments', 'DiscountAmount') IS NULL
BEGIN
    ALTER TABLE dbo.BookingPayments
    ADD DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingPayments_DiscountAmount DEFAULT (0);

    PRINT 'Column DiscountAmount added to BookingPayments';
END
ELSE
BEGIN
    PRINT 'Column DiscountAmount already exists in BookingPayments';
END

IF COL_LENGTH('dbo.BookingPayments', 'DiscountPercent') IS NULL
BEGIN
    ALTER TABLE dbo.BookingPayments
    ADD DiscountPercent DECIMAL(5,2) NULL;

    PRINT 'Column DiscountPercent added to BookingPayments';
END
ELSE
BEGIN
    PRINT 'Column DiscountPercent already exists in BookingPayments';
END

IF COL_LENGTH('dbo.BookingPayments', 'RoundOffAmount') IS NULL
BEGIN
    ALTER TABLE dbo.BookingPayments
    ADD RoundOffAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingPayments_RoundOffAmount DEFAULT (0);

    PRINT 'Column RoundOffAmount added to BookingPayments';
END
ELSE
BEGIN
    PRINT 'Column RoundOffAmount already exists in BookingPayments';
END

IF COL_LENGTH('dbo.BookingPayments', 'IsRoundOffApplied') IS NULL
BEGIN
    ALTER TABLE dbo.BookingPayments
    ADD IsRoundOffApplied BIT NOT NULL CONSTRAINT DF_BookingPayments_IsRoundOffApplied DEFAULT (0);

    PRINT 'Column IsRoundOffApplied added to BookingPayments';
END
ELSE
BEGIN
    PRINT 'Column IsRoundOffApplied already exists in BookingPayments';
END
