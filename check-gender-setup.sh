#!/bin/zsh

echo "🔍 Gender Feature Troubleshooting"
echo "=================================="
echo ""

# Step 1: Check if migration has been run
echo "📋 Step 1: Checking if Gender column exists in database..."
echo ""

sqlcmd -S tcp:198.38.81.123,1433 -d HMS_dev -U HMS_SA -P 'HMS_root_123' -Q "
SELECT 
    'Guests' AS TableName,
    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Guests]') AND name = 'Gender')
        THEN 'EXISTS ✓'
        ELSE 'MISSING ✗ - Run migration!'
    END AS Status
UNION ALL
SELECT 
    'BookingGuests' AS TableName,
    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingGuests]') AND name = 'Gender')
        THEN 'EXISTS ✓'
        ELSE 'MISSING ✗ - Run migration!'
    END AS Status
" 2>/dev/null

if [ $? -ne 0 ]; then
    echo ""
    echo "❌ Could not connect to database or sqlcmd not found"
    echo ""
    echo "To run the migration manually:"
    echo "  cd /Users/abhikporel/dev/Hotelapp/Database/Scripts"
    echo "  sqlcmd -S tcp:198.38.81.123,1433 -d HMS_dev -U HMS_SA -P 'HMS_root_123' -i 26_AddGenderColumn.sql"
    exit 1
fi

echo ""
echo "📝 If Gender column is MISSING, run the migration:"
echo "   cd /Users/abhikporel/dev/Hotelapp/Database/Scripts"
echo "   ./run-migration.sh 26_AddGenderColumn.sql"
echo ""
echo "🔄 After running migration, restart your application:"
echo "   Ctrl+C to stop, then: dotnet run --launch-profile http"
echo ""
