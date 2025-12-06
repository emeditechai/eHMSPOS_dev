-- Extend Guests and BookingGuests tables to capture structured address information

-- Guests table modifications
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Guests') AND name = 'Pincode')
BEGIN
    ALTER TABLE Guests ADD Pincode NVARCHAR(20) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Guests') AND name = 'CountryId')
BEGIN
    ALTER TABLE Guests ADD CountryId INT NULL;
    ALTER TABLE Guests WITH CHECK
        ADD CONSTRAINT FK_Guests_Countries FOREIGN KEY (CountryId) REFERENCES Countries(Id);
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Guests') AND name = 'StateId')
BEGIN
    ALTER TABLE Guests ADD StateId INT NULL;
    ALTER TABLE Guests WITH CHECK
        ADD CONSTRAINT FK_Guests_States FOREIGN KEY (StateId) REFERENCES States(Id);
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Guests') AND name = 'CityId')
BEGIN
    ALTER TABLE Guests ADD CityId INT NULL;
    ALTER TABLE Guests WITH CHECK
        ADD CONSTRAINT FK_Guests_Cities FOREIGN KEY (CityId) REFERENCES Cities(Id);
END;

-- BookingGuests table modifications
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'Address')
BEGIN
    ALTER TABLE BookingGuests ADD Address NVARCHAR(250) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'City')
BEGIN
    ALTER TABLE BookingGuests ADD City NVARCHAR(100) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'State')
BEGIN
    ALTER TABLE BookingGuests ADD State NVARCHAR(100) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'Country')
BEGIN
    ALTER TABLE BookingGuests ADD Country NVARCHAR(100) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'Pincode')
BEGIN
    ALTER TABLE BookingGuests ADD Pincode NVARCHAR(20) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'CountryId')
BEGIN
    ALTER TABLE BookingGuests ADD CountryId INT NULL;
    ALTER TABLE BookingGuests WITH CHECK
        ADD CONSTRAINT FK_BookingGuests_Countries FOREIGN KEY (CountryId) REFERENCES Countries(Id);
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'StateId')
BEGIN
    ALTER TABLE BookingGuests ADD StateId INT NULL;
    ALTER TABLE BookingGuests WITH CHECK
        ADD CONSTRAINT FK_BookingGuests_States FOREIGN KEY (StateId) REFERENCES States(Id);
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'CityId')
BEGIN
    ALTER TABLE BookingGuests ADD CityId INT NULL;
    ALTER TABLE BookingGuests WITH CHECK
        ADD CONSTRAINT FK_BookingGuests_Cities FOREIGN KEY (CityId) REFERENCES Cities(Id);
END;

PRINT 'Guest address schema updated successfully.';
