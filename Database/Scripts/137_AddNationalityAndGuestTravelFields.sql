-- Migration 137: Add Nationalities table and PurposeOfVisit, ComingFrom, GoingTo, NationalityId to Guests and BookingGuests
-- =============================================

-- 1. Create Nationalities master table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Nationalities') AND type = 'U')
BEGIN
    CREATE TABLE Nationalities (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Code NVARCHAR(10) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Created Nationalities table.';
END
GO

-- 2. Seed predefined nationalities
IF NOT EXISTS (SELECT 1 FROM Nationalities)
BEGIN
    INSERT INTO Nationalities (Name, Code) VALUES
    ('Indian', 'IN'),
    ('American', 'US'),
    ('British', 'GB'),
    ('Canadian', 'CA'),
    ('Australian', 'AU'),
    ('German', 'DE'),
    ('French', 'FR'),
    ('Japanese', 'JP'),
    ('Chinese', 'CN'),
    ('South Korean', 'KR'),
    ('Russian', 'RU'),
    ('Brazilian', 'BR'),
    ('Italian', 'IT'),
    ('Spanish', 'ES'),
    ('Mexican', 'MX'),
    ('Bangladeshi', 'BD'),
    ('Pakistani', 'PK'),
    ('Sri Lankan', 'LK'),
    ('Nepali', 'NP'),
    ('Thai', 'TH'),
    ('Malaysian', 'MY'),
    ('Singaporean', 'SG'),
    ('Indonesian', 'ID'),
    ('Filipino', 'PH'),
    ('Vietnamese', 'VN'),
    ('Saudi Arabian', 'SA'),
    ('Emirati', 'AE'),
    ('South African', 'ZA'),
    ('Nigerian', 'NG'),
    ('Egyptian', 'EG'),
    ('Turkish', 'TR'),
    ('Dutch', 'NL'),
    ('Swiss', 'CH'),
    ('Swedish', 'SE'),
    ('Norwegian', 'NO'),
    ('Danish', 'DK'),
    ('Finnish', 'FI'),
    ('Irish', 'IE'),
    ('Portuguese', 'PT'),
    ('Polish', 'PL'),
    ('Greek', 'GR'),
    ('Israeli', 'IL'),
    ('Argentinian', 'AR'),
    ('Colombian', 'CO'),
    ('Chilean', 'CL'),
    ('Peruvian', 'PE'),
    ('New Zealander', 'NZ'),
    ('Kenyan', 'KE'),
    ('Bhutanese', 'BT'),
    ('Maldivian', 'MV');
    PRINT 'Seeded 50 nationalities.';
END
GO

-- 3. Add new columns to Guests table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Guests') AND name = 'NationalityId')
BEGIN
    ALTER TABLE Guests ADD NationalityId INT NULL;
    PRINT 'Added NationalityId to Guests.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Guests') AND name = 'PurposeOfVisit')
BEGIN
    ALTER TABLE Guests ADD PurposeOfVisit NVARCHAR(200) NULL;
    PRINT 'Added PurposeOfVisit to Guests.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Guests') AND name = 'ComingFrom')
BEGIN
    ALTER TABLE Guests ADD ComingFrom NVARCHAR(200) NULL;
    PRINT 'Added ComingFrom to Guests.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Guests') AND name = 'GoingTo')
BEGIN
    ALTER TABLE Guests ADD GoingTo NVARCHAR(200) NULL;
    PRINT 'Added GoingTo to Guests.';
END
GO

-- 4. Add new columns to BookingGuests table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'NationalityId')
BEGIN
    ALTER TABLE BookingGuests ADD NationalityId INT NULL;
    PRINT 'Added NationalityId to BookingGuests.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'PurposeOfVisit')
BEGIN
    ALTER TABLE BookingGuests ADD PurposeOfVisit NVARCHAR(200) NULL;
    PRINT 'Added PurposeOfVisit to BookingGuests.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'ComingFrom')
BEGIN
    ALTER TABLE BookingGuests ADD ComingFrom NVARCHAR(200) NULL;
    PRINT 'Added ComingFrom to BookingGuests.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'GoingTo')
BEGIN
    ALTER TABLE BookingGuests ADD GoingTo NVARCHAR(200) NULL;
    PRINT 'Added GoingTo to BookingGuests.';
END
GO

PRINT 'Migration 137 complete.';
