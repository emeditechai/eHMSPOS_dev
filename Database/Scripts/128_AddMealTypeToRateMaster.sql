-- Migration: Add MealType column to RateMaster table
-- Meal types: EP (European Plan - Room Only), CP (Continental Plan - Breakfast), 
--             MAP (Modified American Plan - Breakfast+Dinner), AP (American Plan - All Meals)

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('RateMaster') AND name = 'MealType')
BEGIN
    ALTER TABLE [dbo].[RateMaster]
    ADD [MealType] NVARCHAR(10) NULL DEFAULT 'EP';

    PRINT 'Added MealType column to RateMaster table';
END
ELSE
BEGIN
    PRINT 'MealType column already exists in RateMaster table';
END
GO
