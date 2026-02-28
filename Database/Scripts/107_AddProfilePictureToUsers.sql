-- Add ProfilePicturePath column to Users table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'ProfilePicturePath')
BEGIN
    ALTER TABLE Users ADD ProfilePicturePath NVARCHAR(500) NULL;
    PRINT 'ProfilePicturePath column added to Users table.';
END
ELSE
BEGIN
    PRINT 'ProfilePicturePath column already exists.';
END
