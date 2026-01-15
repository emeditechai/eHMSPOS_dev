-- Guest Feedback tables (Hotel Feedback)

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'dbo' AND t.name = 'GuestFeedback'
)
BEGIN
    CREATE TABLE dbo.GuestFeedback (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BranchID INT NOT NULL,

        BookingId INT NULL,
        BookingNumber NVARCHAR(50) NULL,
        RoomNumber NVARCHAR(20) NULL,
        VisitDate DATE NOT NULL CONSTRAINT DF_GuestFeedback_VisitDate DEFAULT (CAST(GETDATE() AS DATE)),

        GuestName NVARCHAR(120) NULL,
        Email NVARCHAR(120) NULL,
        Phone NVARCHAR(30) NULL,
        Birthday DATE NULL,
        Anniversary DATE NULL,
        IsFirstVisit BIT NULL,

        OverallRating TINYINT NOT NULL,
        RoomCleanlinessRating TINYINT NULL,
        StaffBehaviorRating TINYINT NULL,
        ServiceRating TINYINT NULL,
        RoomComfortRating TINYINT NULL,
        AmenitiesRating TINYINT NULL,
        FoodRating TINYINT NULL,
        ValueForMoneyRating TINYINT NULL,
        CheckInExperienceRating TINYINT NULL,

        QuickTags NVARCHAR(500) NULL,
        Comments NVARCHAR(1000) NULL,

        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_GuestFeedback_CreatedDate DEFAULT (GETDATE())
    );

    CREATE INDEX IX_GuestFeedback_BranchID_VisitDate
        ON dbo.GuestFeedback (BranchID, VisitDate DESC, Id DESC);

    CREATE INDEX IX_GuestFeedback_BookingNumber
        ON dbo.GuestFeedback (BookingNumber);
END
