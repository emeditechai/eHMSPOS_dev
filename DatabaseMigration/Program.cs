using System.Data.SqlClient;

string connectionString = "Server=tcp:198.38.81.123,1433;Database=HMS_dev;User Id=HMS_SA;Password=HMS_root_123;TrustServerCertificate=True;";

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
        Console.WriteLine($"✓ Server: 198.38.81.123");
        Console.WriteLine($"✓ Database: HMS_dev");
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
