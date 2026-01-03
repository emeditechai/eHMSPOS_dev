-- Run this script in SQL Server Management Studio or your SQL client
-- This updates the sp_GetGstReport stored procedure to include missing columns

USE [HMS_POS];  -- Replace with your actual database name
GO

EXEC('DROP PROCEDURE IF EXISTS dbo.sp_GetGstReport');
GO
