-- Check if Gender column exists in both tables
SELECT 
    'Guests' AS TableName,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Guests' AND COLUMN_NAME = 'Gender'

UNION ALL

SELECT 
    'BookingGuests' AS TableName,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'BookingGuests' AND COLUMN_NAME = 'Gender';

-- If the columns exist, show sample data
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Guests]') AND name = 'Gender')
BEGIN
    PRINT 'Gender column exists in Guests table';
    SELECT TOP 5 Id, FirstName, LastName, Gender FROM Guests WHERE Gender IS NOT NULL;
END
ELSE
BEGIN
    PRINT 'Gender column does NOT exist in Guests table - Please run migration script 26_AddGenderColumn.sql';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingGuests]') AND name = 'Gender')
BEGIN
    PRINT 'Gender column exists in BookingGuests table';
END
ELSE
BEGIN
    PRINT 'Gender column does NOT exist in BookingGuests table - Please run migration script 26_AddGenderColumn.sql';
END
