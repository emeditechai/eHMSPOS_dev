-- ============================================================
-- View  : dbo.vw_BranchHotelSettings
-- Purpose: Fetches Branch-wise Hotel Settings details.
-- ============================================================

IF OBJECT_ID('dbo.vw_BranchHotelSettings', 'V') IS NOT NULL
    DROP VIEW dbo.vw_BranchHotelSettings;
GO

CREATE VIEW dbo.vw_BranchHotelSettings
AS
SELECT 
    b.BranchID AS [Branch ID],
    b.BranchName AS [Branch Name],
    hs.HotelName AS [Hotel Name],
    hs.GSTCode AS [GST Code],
    hs.Address AS [Address],
    hs.ContactNumber1 AS [Contact Number 1],
    hs.ContactNumber2 AS [Contact Number 2],
    hs.EmailAddress AS [Email Address],
    hs.Website AS [Website],
    hs.PoliceStation AS [Police Station],
    hs.CheckInTime AS [Check-In Time],
    hs.CheckOutTime AS [Check-Out Time],
    hs.LogoPath AS [Logo Path]
FROM 
    dbo.BranchMaster b
LEFT JOIN 
    dbo.HotelSettings hs ON b.BranchID = hs.BranchID AND hs.IsActive = 1
WHERE 
    b.IsActive = 1;
GO
