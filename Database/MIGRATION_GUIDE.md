# Database Migration Guide

## Running Migrations with sqlcmd

### Prerequisites
Ensure `sqlcmd` is installed on your system:
- **macOS**: `brew install sqlcmd` or `brew install mssql-tools`
- **Windows**: Included with SQL Server tools
- **Linux**: Install `mssql-tools` package

### Method 1: Using the Shell Script (Recommended)

```bash
cd /Users/abhikporel/dev/Hotelapp/Database/Scripts

# Make script executable (first time only)
chmod +x run-migration.sh

# Run specific migration
./run-migration.sh 26_AddGenderColumn.sql
```

### Method 2: Direct sqlcmd Command

```bash
cd /Users/abhikporel/dev/Hotelapp/Database/Scripts

sqlcmd -S tcp:198.38.81.123,1433 \
       -d HMS_dev \
       -U HMS_SA \
       -P 'HMS_root_123' \
       -i 26_AddGenderColumn.sql
```

### Method 3: Run All Scripts in Order

```bash
cd /Users/abhikporel/dev/Hotelapp/Database/Scripts

for script in *.sql; do
    echo "Running $script..."
    sqlcmd -S tcp:198.38.81.123,1433 -d HMS_dev -U HMS_SA -P 'HMS_root_123' -i "$script"
done
```

## Current Pending Migration

**26_AddGenderColumn.sql** - Adds Gender column to Guests and BookingGuests tables

Run this migration before testing the Gender feature:
```bash
cd /Users/abhikporel/dev/Hotelapp/Database/Scripts
./run-migration.sh 26_AddGenderColumn.sql
```

## Troubleshooting

### sqlcmd not found
```bash
# macOS
brew install sqlcmd

# Or install full SQL Server tools
brew tap microsoft/mssql-release https://github.com/Microsoft/homebrew-mssql-release
brew install mssql-tools
```

### Connection issues
- Verify server IP: `198.38.81.123:1433`
- Verify database: `HMS_dev`
- Check network connectivity
- Ensure SQL Server allows remote connections

### Permission denied on script
```bash
chmod +x run-migration.sh
```
