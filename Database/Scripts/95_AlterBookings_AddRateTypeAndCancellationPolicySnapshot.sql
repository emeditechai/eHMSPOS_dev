-- =============================================
-- Add RateType + cancellation policy snapshot fields to Bookings
-- Created: 2026-02-08
-- =============================================

IF COL_LENGTH('dbo.Bookings', 'RateType') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings
    ADD RateType NVARCHAR(20) NOT NULL CONSTRAINT DF_Bookings_RateType DEFAULT ('Standard');

    PRINT 'Column RateType added to Bookings';
END
ELSE
BEGIN
    PRINT 'Column RateType already exists in Bookings';
END
GO

IF COL_LENGTH('dbo.Bookings', 'CancellationPolicyId') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings
    ADD CancellationPolicyId INT NULL;

    PRINT 'Column CancellationPolicyId added to Bookings';
END
ELSE
BEGIN
    PRINT 'Column CancellationPolicyId already exists in Bookings';
END
GO

IF COL_LENGTH('dbo.Bookings', 'CancellationPolicySnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings
    ADD CancellationPolicySnapshot NVARCHAR(MAX) NULL;

    PRINT 'Column CancellationPolicySnapshot added to Bookings';
END
ELSE
BEGIN
    PRINT 'Column CancellationPolicySnapshot already exists in Bookings';
END
GO

IF COL_LENGTH('dbo.Bookings', 'CancellationPolicyAccepted') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings
    ADD CancellationPolicyAccepted BIT NOT NULL CONSTRAINT DF_Bookings_CancellationPolicyAccepted DEFAULT (0);

    PRINT 'Column CancellationPolicyAccepted added to Bookings';
END
ELSE
BEGIN
    PRINT 'Column CancellationPolicyAccepted already exists in Bookings';
END
GO

IF COL_LENGTH('dbo.Bookings', 'CancellationPolicyAcceptedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings
    ADD CancellationPolicyAcceptedAt DATETIME2 NULL;

    PRINT 'Column CancellationPolicyAcceptedAt added to Bookings';
END
ELSE
BEGIN
    PRINT 'Column CancellationPolicyAcceptedAt already exists in Bookings';
END
GO
