-- Add GuestId column to BookingGuests so documents can be linked from Guest master.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingGuests]') AND name = 'GuestId')
BEGIN
    ALTER TABLE [dbo].[BookingGuests]
        ADD [GuestId] INT NULL;

    PRINT 'Added GuestId column to BookingGuests';
END
ELSE
BEGIN
    PRINT 'GuestId column already exists in BookingGuests';
END

-- Optional: backfill GuestId using phone match (best-effort)
-- (Only fills when BookingGuests.Phone matches an active Guest)
-- Use dynamic SQL so this script compiles even before GuestId exists.
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingGuests]') AND name = 'GuestId')
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'
        UPDATE bg
        SET bg.GuestId = g.Id
        FROM BookingGuests bg
        INNER JOIN Guests g ON g.Phone = bg.Phone AND g.IsActive = 1
        WHERE bg.GuestId IS NULL;';

    EXEC sp_executesql @sql;
    PRINT 'Backfilled BookingGuests.GuestId from Guests by phone where possible';
END

-- Add FK + index (safe checks)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_BookingGuests_Guests_GuestId')
BEGIN
    ALTER TABLE [dbo].[BookingGuests]
        ADD CONSTRAINT [FK_BookingGuests_Guests_GuestId]
        FOREIGN KEY ([GuestId]) REFERENCES [dbo].[Guests]([Id]);

    PRINT 'Added FK FK_BookingGuests_Guests_GuestId';
END
ELSE
BEGIN
    PRINT 'FK FK_BookingGuests_Guests_GuestId already exists';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BookingGuests_GuestId' AND object_id = OBJECT_ID(N'[dbo].[BookingGuests]'))
BEGIN
    CREATE INDEX [IX_BookingGuests_GuestId] ON [dbo].[BookingGuests]([GuestId]);
    PRINT 'Created index IX_BookingGuests_GuestId';
END
ELSE
BEGIN
    PRINT 'Index IX_BookingGuests_GuestId already exists';
END
