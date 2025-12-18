-- =============================================
-- Hotel Management System - Transaction Data Cleaner
-- Created: 2025-12-07
-- Description: Removes all transaction data (Bookings, Payments, Guests) while preserving Master Data
-- WARNING: This script will DELETE all booking, payment, and guest records permanently!
-- =============================================

USE HMS_dev;
GO

PRINT '========================================';
PRINT 'TRANSACTION DATA CLEANER';
PRINT 'Started at: ' + CONVERT(VARCHAR(50), GETDATE(), 121);
PRINT '========================================';
PRINT '';

-- =============================================
-- Step 1: Disable Foreign Key Constraints
-- =============================================
PRINT 'Step 1: Disabling foreign key constraints...';
ALTER TABLE BookingRooms NOCHECK CONSTRAINT ALL;
ALTER TABLE BookingAuditLog NOCHECK CONSTRAINT ALL;
ALTER TABLE BookingPayments NOCHECK CONSTRAINT ALL;
ALTER TABLE BookingOtherCharges NOCHECK CONSTRAINT ALL;
ALTER TABLE BookingGuests NOCHECK CONSTRAINT ALL;
ALTER TABLE BookingRoomNights NOCHECK CONSTRAINT ALL;
ALTER TABLE ReservationRoomNights NOCHECK CONSTRAINT ALL;
ALTER TABLE Bookings NOCHECK CONSTRAINT ALL;
PRINT '✓ Foreign key constraints disabled';
PRINT '';

-- =============================================
-- Step 2: Count existing records (for reporting)
-- =============================================
PRINT 'Step 2: Counting existing transaction records...';

DECLARE @BookingRoomsCount INT;
DECLARE @BookingAuditLogCount INT;
DECLARE @BookingPaymentsCount INT;
DECLARE @BookingOtherChargesCount INT;
DECLARE @BookingGuestsCount INT;
DECLARE @BookingRoomNightsCount INT;
DECLARE @ReservationRoomNightsCount INT;
DECLARE @BookingsCount INT;
DECLARE @GuestsCount INT;

SELECT @BookingRoomsCount = COUNT(*) FROM BookingRooms;
SELECT @BookingAuditLogCount = COUNT(*) FROM BookingAuditLog;
SELECT @BookingPaymentsCount = COUNT(*) FROM BookingPayments;
SELECT @BookingOtherChargesCount = COUNT(*) FROM BookingOtherCharges;
SELECT @BookingGuestsCount = COUNT(*) FROM BookingGuests;
SELECT @BookingRoomNightsCount = COUNT(*) FROM BookingRoomNights;
SELECT @ReservationRoomNightsCount = COUNT(*) FROM ReservationRoomNights;
SELECT @BookingsCount = COUNT(*) FROM Bookings;
SELECT @GuestsCount = COUNT(*) FROM Guests;

PRINT '  - BookingRooms: ' + CAST(@BookingRoomsCount AS VARCHAR(10)) + ' records';
PRINT '  - BookingAuditLog: ' + CAST(@BookingAuditLogCount AS VARCHAR(10)) + ' records';
PRINT '  - BookingPayments: ' + CAST(@BookingPaymentsCount AS VARCHAR(10)) + ' records';
PRINT '  - BookingOtherCharges: ' + CAST(@BookingOtherChargesCount AS VARCHAR(10)) + ' records';
PRINT '  - BookingGuests: ' + CAST(@BookingGuestsCount AS VARCHAR(10)) + ' records';
PRINT '  - BookingRoomNights: ' + CAST(@BookingRoomNightsCount AS VARCHAR(10)) + ' records';
PRINT '  - ReservationRoomNights: ' + CAST(@ReservationRoomNightsCount AS VARCHAR(10)) + ' records';
PRINT '  - Bookings: ' + CAST(@BookingsCount AS VARCHAR(10)) + ' records';
PRINT '  - Guests: ' + CAST(@GuestsCount AS VARCHAR(10)) + ' records';
PRINT '';

-- =============================================
-- Step 3: Delete transaction data in correct order
-- =============================================
PRINT 'Step 3: Deleting transaction data...';

-- Delete BookingRooms (multi-room assignments)
DELETE FROM BookingRooms;
PRINT '  ✓ Deleted ' + CAST(@BookingRoomsCount AS VARCHAR(10)) + ' BookingRooms records';

-- Delete BookingAuditLog (audit trail)
DELETE FROM BookingAuditLog;
PRINT '  ✓ Deleted ' + CAST(@BookingAuditLogCount AS VARCHAR(10)) + ' BookingAuditLog records';

-- Delete BookingPayments (payment transactions)
DELETE FROM BookingPayments;
PRINT '  ✓ Deleted ' + CAST(@BookingPaymentsCount AS VARCHAR(10)) + ' BookingPayments records';

-- Delete BookingOtherCharges (additional charges)
DELETE FROM BookingOtherCharges;
PRINT '  ✓ Deleted ' + CAST(@BookingOtherChargesCount AS VARCHAR(10)) + ' BookingOtherCharges records';

-- Delete BookingGuests (additional guests)
DELETE FROM BookingGuests;
PRINT '  ✓ Deleted ' + CAST(@BookingGuestsCount AS VARCHAR(10)) + ' BookingGuests records';

-- Delete BookingRoomNights (room-night inventory)
DELETE FROM BookingRoomNights;
PRINT '  ✓ Deleted ' + CAST(@BookingRoomNightsCount AS VARCHAR(10)) + ' BookingRoomNights records';

-- Delete ReservationRoomNights (pre-assignment nightly breakdown)
DELETE FROM ReservationRoomNights;
PRINT '  ✓ Deleted ' + CAST(@ReservationRoomNightsCount AS VARCHAR(10)) + ' ReservationRoomNights records';

-- Delete Bookings (main booking records)
DELETE FROM Bookings;
PRINT '  ✓ Deleted ' + CAST(@BookingsCount AS VARCHAR(10)) + ' Bookings records';

-- Delete Guests (all guest records)
DELETE FROM Guests;
PRINT '  ✓ Deleted ' + CAST(@GuestsCount AS VARCHAR(10)) + ' Guests records';
PRINT '';

-- =============================================
-- Step 4: Reset Room Statuses
-- =============================================
PRINT 'Step 4: Resetting all room statuses to Available...';

DECLARE @RoomsUpdated INT;

UPDATE Rooms 
SET Status = 'Available'
WHERE Status != 'Available';

SELECT @RoomsUpdated = @@ROWCOUNT;
PRINT '  ✓ Updated ' + CAST(@RoomsUpdated AS VARCHAR(10)) + ' rooms to Available status';
PRINT '';

-- =============================================
-- Step 5: Re-enable Foreign Key Constraints
-- =============================================
PRINT 'Step 5: Re-enabling foreign key constraints...';
ALTER TABLE BookingRooms CHECK CONSTRAINT ALL;
ALTER TABLE BookingAuditLog CHECK CONSTRAINT ALL;
ALTER TABLE BookingPayments CHECK CONSTRAINT ALL;
ALTER TABLE BookingOtherCharges CHECK CONSTRAINT ALL;
ALTER TABLE BookingGuests CHECK CONSTRAINT ALL;
ALTER TABLE BookingRoomNights CHECK CONSTRAINT ALL;
ALTER TABLE ReservationRoomNights CHECK CONSTRAINT ALL;
ALTER TABLE Bookings CHECK CONSTRAINT ALL;
PRINT '✓ Foreign key constraints re-enabled';
PRINT '';

-- =============================================
-- Step 6: Reset Identity Seeds
-- =============================================
PRINT 'Step 6: Resetting identity seeds to start from 1...';

DBCC CHECKIDENT ('BookingRooms', RESEED, 0);
DBCC CHECKIDENT ('BookingAuditLog', RESEED, 0);
DBCC CHECKIDENT ('BookingPayments', RESEED, 0);
DBCC CHECKIDENT ('BookingOtherCharges', RESEED, 0);
DBCC CHECKIDENT ('BookingGuests', RESEED, 0);
DBCC CHECKIDENT ('BookingRoomNights', RESEED, 0);
DBCC CHECKIDENT ('ReservationRoomNights', RESEED, 0);
DBCC CHECKIDENT ('Bookings', RESEED, 0);
DBCC CHECKIDENT ('Guests', RESEED, 0);

PRINT '  ✓ Identity seeds reset for all transaction tables';
PRINT '';

-- =============================================
-- Step 7: Verify Master Data is Preserved
-- =============================================
PRINT 'Step 7: Verifying master data preservation...';

DECLARE @UsersCount INT, @RoomTypesCount INT, @RoomsCount INT, @FloorsCount INT;
DECLARE @BranchesCount INT, @RateMasterCount INT, @BanksCount INT;

SELECT @UsersCount = COUNT(*) FROM Users WHERE IsActive = 1;
SELECT @RoomTypesCount = COUNT(*) FROM RoomTypes WHERE IsActive = 1;
SELECT @RoomsCount = COUNT(*) FROM Rooms WHERE IsActive = 1;
SELECT @FloorsCount = COUNT(*) FROM Floors WHERE IsActive = 1;
SELECT @BranchesCount = COUNT(*) FROM BranchMaster WHERE IsActive = 1;
SELECT @RateMasterCount = COUNT(*) FROM RateMaster WHERE IsActive = 1;
SELECT @BanksCount = COUNT(*) FROM Banks WHERE IsActive = 1;

PRINT '  Master Data Counts (Preserved):';
PRINT '    - Users: ' + CAST(@UsersCount AS VARCHAR(10));
PRINT '    - Branches: ' + CAST(@BranchesCount AS VARCHAR(10));
PRINT '    - Room Types: ' + CAST(@RoomTypesCount AS VARCHAR(10));
PRINT '    - Rooms: ' + CAST(@RoomsCount AS VARCHAR(10));
PRINT '    - Floors: ' + CAST(@FloorsCount AS VARCHAR(10));
PRINT '    - Rate Master: ' + CAST(@RateMasterCount AS VARCHAR(10));
PRINT '    - Banks: ' + CAST(@BanksCount AS VARCHAR(10));
PRINT '';

-- =============================================
-- Step 8: Final Verification
-- =============================================
PRINT 'Step 8: Final verification...';

DECLARE @RemainingBookings INT, @RemainingPayments INT, @RemainingRooms INT;

SELECT @RemainingBookings = COUNT(*) FROM Bookings;
SELECT @RemainingPayments = COUNT(*) FROM BookingPayments;
SELECT @RemainingRooms = COUNT(*) FROM BookingRooms;

IF @RemainingBookings = 0 AND @RemainingPayments = 0 AND @RemainingRooms = 0
BEGIN
    PRINT '  ✓ All transaction data successfully removed';
    PRINT '  ✓ System is clean and ready for new bookings';
END
ELSE
BEGIN
    PRINT '  ⚠ WARNING: Some transaction data may remain:';
    PRINT '    - Remaining Bookings: ' + CAST(@RemainingBookings AS VARCHAR(10));
    PRINT '    - Remaining Payments: ' + CAST(@RemainingPayments AS VARCHAR(10));
    PRINT '    - Remaining Room Assignments: ' + CAST(@RemainingRooms AS VARCHAR(10));
END

PRINT '';
PRINT '========================================';
PRINT 'TRANSACTION DATA CLEANER COMPLETED';
PRINT 'Finished at: ' + CONVERT(VARCHAR(50), GETDATE(), 121);
PRINT '========================================';
PRINT '';
PRINT 'SUMMARY:';
PRINT '  Total Records Deleted: ' + CAST(@BookingsCount + @BookingPaymentsCount + @BookingOtherChargesCount + @BookingGuestsCount + @BookingRoomNightsCount + @ReservationRoomNightsCount + @BookingAuditLogCount + @BookingRoomsCount + @GuestsCount AS VARCHAR(10));
PRINT '  Rooms Reset: ' + CAST(@RoomsUpdated AS VARCHAR(10));
PRINT '  Master Data: PRESERVED';
PRINT '';
PRINT '✓ System is ready for fresh transaction data';
GO

