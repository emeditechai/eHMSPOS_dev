#!/bin/zsh

# Database Migration Script Runner
# Usage: ./run-migration.sh 26_AddGenderColumn.sql

SCRIPT_FILE=${1:-"26_AddGenderColumn.sql"}
SERVER=${DB_SERVER:-"tcp:127.0.0.1,1433"}
DATABASE=${DB_NAME:-"HotelApp"}
USERNAME=${DB_USER:-"sa"}
PASSWORD=${DB_PASSWORD:-""}

if [ -z "$PASSWORD" ]; then
    echo "❌ DB_PASSWORD is not set. Example:"
    echo "   export DB_SERVER='tcp:127.0.0.1,1433'"
    echo "   export DB_NAME='HotelApp'"
    echo "   export DB_USER='sa'"
    echo "   export DB_PASSWORD='your_password'"
    exit 1
fi

echo "🔄 Running SQL migration: $SCRIPT_FILE"
echo "📦 Database: $DATABASE"
echo ""

sqlcmd -C -S "$SERVER" -d "$DATABASE" -U "$USERNAME" -P "$PASSWORD" -i "$SCRIPT_FILE"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Migration completed successfully!"
else
    echo ""
    echo "❌ Migration failed. Please check the error above."
    exit 1
fi
