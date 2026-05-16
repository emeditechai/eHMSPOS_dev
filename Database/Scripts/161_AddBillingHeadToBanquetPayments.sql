-- ============================================================
-- Script 161: Add BillingHead and ReceiptGroupNumber columns
--             to BanquetBookingPayments for head-wise payment
--             allocation (V=Venue, P=Package, A=Addon).
-- ============================================================

-- BillingHead: nullable single-char code V / P / A
IF COL_LENGTH('dbo.BanquetBookingPayments', 'BillingHead') IS NULL
BEGIN
    ALTER TABLE dbo.BanquetBookingPayments
        ADD BillingHead NVARCHAR(1) NULL;
    PRINT 'Added BillingHead to BanquetBookingPayments';
END
ELSE
    PRINT 'BillingHead already exists; skipping.';
GO

-- ReceiptGroupNumber: groups multiple head-split rows into one logical receipt
IF COL_LENGTH('dbo.BanquetBookingPayments', 'ReceiptGroupNumber') IS NULL
BEGIN
    ALTER TABLE dbo.BanquetBookingPayments
        ADD ReceiptGroupNumber NVARCHAR(50) NULL;
    PRINT 'Added ReceiptGroupNumber to BanquetBookingPayments';
END
ELSE
    PRINT 'ReceiptGroupNumber already exists; skipping.';
GO
