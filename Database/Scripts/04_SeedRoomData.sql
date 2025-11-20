-- =============================================
-- Hotel Management System - Room Management Seed Data
-- Created: 2025-11-19
-- Description: Initial data for room types and rooms
-- =============================================

USE HotelApp;
GO

-- =============================================
-- Seed: RoomTypes
-- =============================================
IF NOT EXISTS (SELECT * FROM RoomTypes WHERE TypeName = 'Standard')
BEGIN
    INSERT INTO RoomTypes (TypeName, Description, BaseRate, MaxOccupancy, Amenities)
    VALUES 
        ('Standard', 'Standard room with basic amenities', 2500.00, 2, 'AC, Wi-Fi, TV, Bathroom'),
        ('Deluxe', 'Deluxe room with premium amenities', 4500.00, 3, 'AC, Wi-Fi, TV, Mini Bar, Bathroom, Balcony'),
        ('Suite', 'Luxury suite with separate living area', 8500.00, 4, 'AC, Wi-Fi, TV, Mini Bar, Kitchenette, Living Area, 2 Bathrooms, Balcony');
        
    PRINT 'Room types seeded successfully';
END
GO

-- =============================================
-- Seed: Rooms (Floor 1)
-- =============================================
DECLARE @StandardId INT = (SELECT Id FROM RoomTypes WHERE TypeName = 'Standard');
DECLARE @DeluxeId INT = (SELECT Id FROM RoomTypes WHERE TypeName = 'Deluxe');
DECLARE @SuiteId INT = (SELECT Id FROM RoomTypes WHERE TypeName = 'Suite');

IF NOT EXISTS (SELECT * FROM Rooms WHERE RoomNumber = '101')
BEGIN
    INSERT INTO Rooms (RoomNumber, RoomTypeId, Floor, Status)
    VALUES 
        ('101', @StandardId, 1, 'Available'),
        ('102', @StandardId, 1, 'Occupied'),
        ('103', @DeluxeId, 1, 'Available'),
        ('104', @DeluxeId, 1, 'Cleaning'),
        ('105', @SuiteId, 1, 'Available'),
        ('106', @StandardId, 1, 'Available'),
        ('107', @StandardId, 1, 'Maintenance'),
        ('108', @DeluxeId, 1, 'Available'),
        ('109', @DeluxeId, 1, 'Occupied'),
        ('110', @SuiteId, 1, 'Available');
        
    PRINT 'Floor 1 rooms seeded successfully';
END
GO

-- =============================================
-- Seed: Rooms (Floor 2)
-- =============================================
DECLARE @StandardId INT = (SELECT Id FROM RoomTypes WHERE TypeName = 'Standard');
DECLARE @DeluxeId INT = (SELECT Id FROM RoomTypes WHERE TypeName = 'Deluxe');
DECLARE @SuiteId INT = (SELECT Id FROM RoomTypes WHERE TypeName = 'Suite');

IF NOT EXISTS (SELECT * FROM Rooms WHERE RoomNumber = '201')
BEGIN
    INSERT INTO Rooms (RoomNumber, RoomTypeId, Floor, Status)
    VALUES 
        ('201', @StandardId, 2, 'Available'),
        ('202', @StandardId, 2, 'Available'),
        ('203', @DeluxeId, 2, 'Occupied'),
        ('204', @DeluxeId, 2, 'Available'),
        ('205', @SuiteId, 2, 'Available'),
        ('206', @StandardId, 2, 'Cleaning'),
        ('207', @StandardId, 2, 'Available'),
        ('208', @DeluxeId, 2, 'Available'),
        ('209', @DeluxeId, 2, 'Available'),
        ('210', @SuiteId, 2, 'Occupied');
        
    PRINT 'Floor 2 rooms seeded successfully';
END
GO

-- =============================================
-- Seed: RateTypes
-- =============================================
IF NOT EXISTS (SELECT * FROM RateTypes WHERE CustomerType = 'B2C')
BEGIN
    INSERT INTO RateTypes (CustomerType, Source, Description)
    VALUES 
        ('B2C', 'Walk-in', 'Direct walk-in customers'),
        ('B2C', 'Online', 'Online booking through website'),
        ('B2C', 'Phone', 'Phone reservation'),
        ('B2B', 'Corporate', 'Corporate clients'),
        ('B2B', 'Travel Agent', 'Travel agency bookings'),
        ('B2B', 'OTA', 'Online Travel Agency (MakeMyTrip, Booking.com)');
        
    PRINT 'Rate types seeded successfully';
END
GO

-- =============================================
-- Seed: RateMaster (Sample rates)
-- =============================================
DECLARE @StandardId INT = (SELECT Id FROM RoomTypes WHERE TypeName = 'Standard');
DECLARE @DeluxeId INT = (SELECT Id FROM RoomTypes WHERE TypeName = 'Deluxe');
DECLARE @SuiteId INT = (SELECT Id FROM RoomTypes WHERE TypeName = 'Suite');

IF NOT EXISTS (SELECT * FROM RateMaster WHERE RoomTypeId = @StandardId AND CustomerType = 'B2C')
BEGIN
    INSERT INTO RateMaster (RoomTypeId, CustomerType, Source, BaseRate, ExtraPaxRate, TaxPercentage, StartDate, EndDate, IsWeekdayRate, ApplyDiscount, IsDynamicRate)
    VALUES 
        -- Standard Room Rates
        (@StandardId, 'B2C', 'Walk-in', 2500.00, 500.00, 12.00, '2025-01-01', '2025-12-31', 1, '10%', 0),
        (@StandardId, 'B2C', 'Online', 2300.00, 500.00, 12.00, '2025-01-01', '2025-12-31', 1, '15%', 0),
        (@StandardId, 'B2B', 'Corporate', 2200.00, 400.00, 12.00, '2025-01-01', '2025-12-31', 1, '20%', 0),
        
        -- Deluxe Room Rates
        (@DeluxeId, 'B2C', 'Walk-in', 4500.00, 800.00, 12.00, '2025-01-01', '2025-12-31', 1, '10%', 0),
        (@DeluxeId, 'B2C', 'Online', 4200.00, 800.00, 12.00, '2025-01-01', '2025-12-31', 1, '15%', 0),
        (@DeluxeId, 'B2B', 'Corporate', 4000.00, 700.00, 12.00, '2025-01-01', '2025-12-31', 1, '20%', 0),
        
        -- Suite Rates
        (@SuiteId, 'B2C', 'Walk-in', 8500.00, 1500.00, 12.00, '2025-01-01', '2025-12-31', 1, '10%', 1),
        (@SuiteId, 'B2C', 'Online', 8000.00, 1500.00, 12.00, '2025-01-01', '2025-12-31', 1, '15%', 1),
        (@SuiteId, 'B2B', 'Corporate', 7500.00, 1200.00, 12.00, '2025-01-01', '2025-12-31', 1, '20%', 1);
        
    PRINT 'Rate master seeded successfully';
END
GO

PRINT 'All room management seed data completed successfully';
GO
