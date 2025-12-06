IF COL_LENGTH('dbo.Bookings', 'ActualCheckInDate') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings
    ADD ActualCheckInDate DATETIME NULL;
END
GO
