-- =============================================
-- Hotel Management System - Booking Audit Log
-- Created: 2025-11-20
-- Description: Track all booking changes for audit trail
-- =============================================

USE HMS_dev;
GO

-- =============================================
-- Table: BookingAuditLog
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BookingAuditLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BookingAuditLog] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [BookingId] INT NOT NULL,
        [BookingNumber] NVARCHAR(25) NOT NULL,
        [ActionType] NVARCHAR(50) NOT NULL, -- Created, DatesChanged, RoomAssigned, RoomChanged, PaymentReceived, StatusChanged, Cancelled
        [ActionDescription] NVARCHAR(500) NOT NULL,
        [OldValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [PerformedBy] INT NULL,
        [PerformedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_BookingAuditLog_Bookings FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([Id]) ON DELETE CASCADE,
        CONSTRAINT FK_BookingAuditLog_PerformedBy FOREIGN KEY ([PerformedBy]) REFERENCES [dbo].[Users]([Id])
    );

    CREATE INDEX IX_BookingAuditLog_BookingId ON [dbo].[BookingAuditLog]([BookingId]);
    CREATE INDEX IX_BookingAuditLog_ActionType ON [dbo].[BookingAuditLog]([ActionType]);
    CREATE INDEX IX_BookingAuditLog_PerformedAt ON [dbo].[BookingAuditLog]([PerformedAt]);
END
GO

PRINT 'BookingAuditLog table created successfully';
GO
