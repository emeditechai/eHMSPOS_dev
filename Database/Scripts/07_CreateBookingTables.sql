-- =============================================
-- Hotel Management System - Booking Tables
-- Created: 2025-11-20
-- Description: Booking, payment, and room-night tracking
-- =============================================

USE HMS_dev;
GO

-- =============================================
-- Table: Bookings
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Bookings] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [BookingNumber] NVARCHAR(25) NOT NULL UNIQUE,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [PaymentStatus] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [Channel] NVARCHAR(50) NOT NULL,
        [Source] NVARCHAR(50) NOT NULL,
        [CustomerType] NVARCHAR(50) NOT NULL,
        [CheckInDate] DATE NOT NULL,
        [CheckOutDate] DATE NOT NULL,
        [Nights] INT NOT NULL,
        [RoomTypeId] INT NOT NULL,
        [RoomId] INT NULL,
        [RatePlanId] INT NULL,
        [BaseAmount] DECIMAL(12,2) NOT NULL,
        [TaxAmount] DECIMAL(12,2) NOT NULL,
        [DiscountAmount] DECIMAL(12,2) NOT NULL DEFAULT 0,
        [TotalAmount] DECIMAL(12,2) NOT NULL,
        [DepositAmount] DECIMAL(12,2) NOT NULL DEFAULT 0,
        [BalanceAmount] DECIMAL(12,2) NOT NULL,
        [Adults] INT NOT NULL DEFAULT 1,
        [Children] INT NOT NULL DEFAULT 0,
        [PrimaryGuestFirstName] NVARCHAR(100) NOT NULL,
        [PrimaryGuestLastName] NVARCHAR(100) NOT NULL,
        [PrimaryGuestEmail] NVARCHAR(200) NOT NULL,
        [PrimaryGuestPhone] NVARCHAR(50) NOT NULL,
        [LoyaltyId] NVARCHAR(100) NULL,
        [SpecialRequests] NVARCHAR(1000) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [CreatedBy] INT NULL,
        [LastModifiedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [LastModifiedBy] INT NULL,

        CONSTRAINT FK_Bookings_RoomTypes FOREIGN KEY ([RoomTypeId]) REFERENCES [dbo].[RoomTypes]([Id]),
        CONSTRAINT FK_Bookings_Rooms FOREIGN KEY ([RoomId]) REFERENCES [dbo].[Rooms]([Id]),
        CONSTRAINT FK_Bookings_RatePlan FOREIGN KEY ([RatePlanId]) REFERENCES [dbo].[RateMaster]([Id]),
        CONSTRAINT FK_Bookings_CreatedBy FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT FK_Bookings_LastModifiedBy FOREIGN KEY ([LastModifiedBy]) REFERENCES [dbo].[Users]([Id])
    );

    CREATE INDEX IX_Bookings_CheckIn ON [dbo].[Bookings]([CheckInDate], [CheckOutDate]);
    CREATE INDEX IX_Bookings_Status ON [dbo].[Bookings]([Status]);
END
GO

-- =============================================
-- Table: BookingGuests
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BookingGuests]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BookingGuests] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [BookingId] INT NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [Email] NVARCHAR(200) NULL,
        [Phone] NVARCHAR(50) NULL,
        [GuestType] NVARCHAR(50) NULL,
        [IsPrimary] BIT NOT NULL DEFAULT 0,

        CONSTRAINT FK_BookingGuests_Bookings FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX IX_BookingGuests_BookingId ON [dbo].[BookingGuests]([BookingId]);
END
GO

-- =============================================
-- Table: BookingPayments
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BookingPayments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BookingPayments] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [BookingId] INT NOT NULL,
        [Amount] DECIMAL(12,2) NOT NULL,
        [PaymentMethod] NVARCHAR(50) NOT NULL,
        [PaymentReference] NVARCHAR(200) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Captured',
        [PaidOn] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [Notes] NVARCHAR(500) NULL,

        CONSTRAINT FK_BookingPayments_Bookings FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX IX_BookingPayments_BookingId ON [dbo].[BookingPayments]([BookingId]);
END
GO

-- =============================================
-- Table: BookingRoomNights
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BookingRoomNights]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BookingRoomNights] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [BookingId] INT NOT NULL,
        [RoomId] INT NULL,
        [StayDate] DATE NOT NULL,
        [RateAmount] DECIMAL(12,2) NOT NULL,
        [TaxAmount] DECIMAL(12,2) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Reserved',

        CONSTRAINT FK_BookingRoomNights_Bookings FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([Id]) ON DELETE CASCADE,
        CONSTRAINT FK_BookingRoomNights_Rooms FOREIGN KEY ([RoomId]) REFERENCES [dbo].[Rooms]([Id])
    );

    CREATE INDEX IX_BookingRoomNights_BookingId ON [dbo].[BookingRoomNights]([BookingId]);
    CREATE UNIQUE INDEX IX_BookingRoomNights_RoomDate ON [dbo].[BookingRoomNights]([RoomId], [StayDate]) WHERE [RoomId] IS NOT NULL;
END
GO

PRINT 'Booking tables created successfully';
GO
