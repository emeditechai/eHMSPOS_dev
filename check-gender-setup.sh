#!/bin/zsh

echo "🔍 Gender Feature Troubleshooting"
echo "=================================="
echo ""

# Step 1: Check if migration has been run
echo "📋 Step 1: Checking if Gender column exists in database..."
echo ""

SERVER=${DB_SERVER:-"tcp:127.0.0.1,1433"}
DATABASE=${DB_NAME:-"HotelApp"}
USERNAME=${DB_USER:-"sa"}
PASSWORD=${DB_PASSWORD:-""}

if [ -z "$PASSWORD" ]; then
    echo "❌ DB_PASSWORD is not set. Example:"
    echo "  export DB_SERVER='tcp:127.0.0.1,1433'"
    echo "  export DB_NAME='HotelApp'"
    echo "  export DB_USER='sa'"
    echo "  export DB_PASSWORD='your_password'"
    exit 1
fi

sqlcmd -C -S "$SERVER" -d "$DATABASE" -U "$USERNAME" -P "$PASSWORD" -Q "
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
    echo "  sqlcmd -C -S \"$SERVER\" -d \"$DATABASE\" -U \"$USERNAME\" -P '<db_password>' -i 26_AddGenderColumn.sql"
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
