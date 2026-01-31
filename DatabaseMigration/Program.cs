using System.Data.SqlClient;

var connectionString = Environment.GetEnvironmentVariable("HMS_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("❌ Missing HMS_CONNECTION_STRING env var.");
    Console.WriteLine("Example:");
    Console.WriteLine("  export HMS_CONNECTION_STRING='Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;'");
    Environment.Exit(1);
}

string sql = @"
-- Add Gender column to Guests table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Guests]') AND name = 'Gender')
BEGIN
    ALTER TABLE [dbo].[Guests]
    ADD [Gender] NVARCHAR(20) NULL;
    
    PRINT 'Gender column added to Guests table';
END
ELSE
BEGIN
    PRINT 'Gender column already exists in Guests table';
END

-- Add Gender column to BookingGuests table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingGuests]') AND name = 'Gender')
BEGIN
    ALTER TABLE [dbo].[BookingGuests]
    ADD [Gender] NVARCHAR(20) NULL;
    
    PRINT 'Gender column added to BookingGuests table';
END
ELSE
BEGIN
    PRINT 'Gender column already exists in BookingGuests table';
END

PRINT 'Gender column migration completed successfully';
";

try
{
    using (SqlConnection connection = new SqlConnection(connectionString))
    {
        connection.Open();
        Console.WriteLine("✓ Connected to database successfully.");
        Console.WriteLine();
        
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            command.ExecuteNonQuery();
            Console.WriteLine("✅ Migration script executed successfully!");
            Console.WriteLine();
            Console.WriteLine("Changes applied:");
            Console.WriteLine("  • Gender column added to Guests table");
            Console.WriteLine("  • Gender column added to BookingGuests table");
            Console.WriteLine();
            Console.WriteLine("Gender field is now available across all guest forms:");
            Console.WriteLine("  ✓ Create Booking");
            Console.WriteLine("  ✓ Add Additional Guest");
            Console.WriteLine("  ✓ Edit Guest");
            Console.WriteLine("  ✓ View Guest Details");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    Environment.Exit(1);
}
