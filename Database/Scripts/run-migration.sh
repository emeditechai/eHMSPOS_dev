#!/bin/zsh

# Database Migration Script Runner
# Usage: ./run-migration.sh 26_AddGenderColumn.sql

SCRIPT_FILE=${1:-"26_AddGenderColumn.sql"}
SERVER="tcp:198.38.81.123,1433"
DATABASE="HMS_dev"
USERNAME="sa"
PASSWORD="asdf@1234"

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
