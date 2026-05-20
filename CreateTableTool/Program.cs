using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

var dbPath = @"D:\K8s\CRM_V2_blazor\IzolluCRM\IzolluDayanismaMerkezi\izolluvakfi.db";
using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

Console.WriteLine("\n========== SCHOLARSHIP TRACKING FIX ==========\n");
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("This script will:");
Console.WriteLine("1. DROP the incorrect StudentScholarshipStatuses table");
Console.WriteLine("2. RE-CREATE with correct monthly tracking schema");
Console.WriteLine("3. SEED data for active term (auto-mark past months as PAID)");
Console.ResetColor();
Console.Write("\nPress ENTER to continue or CTRL+C to abort...");
Console.ReadLine();

var cmd = connection.CreateCommand();

// Step 1: Drop existing table
Console.WriteLine("\n[1/4] Dropping incorrect table...");
cmd.CommandText = "DROP TABLE IF EXISTS StudentScholarshipStatuses;";
cmd.ExecuteNonQuery();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("✓ Table dropped");
Console.ResetColor();

// Step 2: Create correct schema
Console.WriteLine("\n[2/4] Creating correct schema with Month/Year columns...");
cmd.CommandText = @"
CREATE TABLE StudentScholarshipStatuses (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    TermId INTEGER NOT NULL,
    Month INTEGER NOT NULL,
    Year INTEGER NOT NULL,
    IsPaid INTEGER NOT NULL DEFAULT 1,
    CutReason TEXT NULL,
    Amount REAL NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL,
    UpdatedBy TEXT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (TermId) REFERENCES Terms(Id)
);";
cmd.ExecuteNonQuery();

// Create index for performance
cmd.CommandText = "CREATE INDEX IX_StudentScholarshipStatuses_StudentId_TermId ON StudentScholarshipStatuses(StudentId, TermId);";
cmd.ExecuteNonQuery();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("✓ Table created with correct schema");
Console.ResetColor();

// Step 3: Get active term and students
Console.WriteLine("\n[3/4] Loading active term and students...");
cmd.CommandText = "SELECT Id, DisplayName, Start FROM Terms WHERE IsActive = 1 LIMIT 1;";
int activeTermId = 0;
string termName = "";
DateTime termStart = DateTime.Now;

using (var reader = cmd.ExecuteReader())
{
    if (reader.Read())
    {
        activeTermId = reader.GetInt32(0);
        termName = reader.GetString(1);
        termStart = DateTime.Parse(reader.GetString(2));
    }
}

if (activeTermId == 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: No active term found!");
    Console.ResetColor();
    return;
}

Console.WriteLine($"  Active Term: {termName} (ID: {activeTermId})");
Console.WriteLine($"  Term Start: {termStart:yyyy-MM-dd}");

// Get all students
cmd.CommandText = "SELECT Id, AdSoyad, AylikTutar FROM Students WHERE MezunMu = 0;";
var students = new List<(int Id, string Name, decimal Amount)>();

using (var reader = cmd.ExecuteReader())
{
    while (reader.Read())
    {
        students.Add((
            reader.GetInt32(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? 3000m : (decimal)reader.GetDouble(2)
        ));
    }
}

Console.WriteLine($"  Found {students.Count} active students");

// Step 4: Seed monthly records
Console.WriteLine("\n[4/4] Seeding monthly scholarship records...");
Console.WriteLine("  Scholarship months: Oct, Nov, Dec, Jan, Feb, Mar, Apr, May");

int startYear = termStart.Year;
var scholarshipMonths = new List<(int Month, int YearOffset)>
{
    (10, 0),  // Ekim - first year
    (11, 0),  // Kasım - first year
    (12, 0),  // Aralık - first year
    (1, 1),   // Ocak - second year
    (2, 1),   // Şubat - second year
    (3, 1),   // Mart - second year
    (4, 1),   // Nisan - second year
    (5, 1)    // Mayıs - second year
};

int totalInserted = 0;
int paidCount = 0;
int pendingCount = 0;
var now = DateTime.Now;

using (var transaction = connection.BeginTransaction())
{
    cmd.Transaction = transaction; // FIX: Assign transaction to command
    
    foreach (var student in students)
    {
        foreach (var (month, yearOffset) in scholarshipMonths)
        {
            int year = startYear + yearOffset;
            var scholarshipDate = new DateTime(year, month, 1);
            
            // Auto-mark as PAID if month is in the past or current month
            bool isCurrentOrPast = scholarshipDate.Year < now.Year || 
                                  (scholarshipDate.Year == now.Year && scholarshipDate.Month <= now.Month);
            int isPaid = isCurrentOrPast ? 1 : 0;
            
            if (isPaid == 1) paidCount++;
            else pendingCount++;
            
            cmd.CommandText = @"
INSERT INTO StudentScholarshipStatuses 
(StudentId, TermId, Month, Year, IsPaid, CutReason, Amount, CreatedAt, UpdatedBy)
VALUES 
(@studentId, @termId, @month, @year, @isPaid, NULL, @amount, @createdAt, 'Auto-Recovery Script');";
            
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@studentId", student.Id);
            cmd.Parameters.AddWithValue("@termId", activeTermId);
            cmd.Parameters.AddWithValue("@month", month);
            cmd.Parameters.AddWithValue("@year", year);
            cmd.Parameters.AddWithValue("@isPaid", isPaid);
            cmd.Parameters.AddWithValue("@amount", student.Amount);
            cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            cmd.ExecuteNonQuery();
            totalInserted++;
        }
    }
    
    transaction.Commit();
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n✓ SUCCESS: Inserted {totalInserted} scholarship records");
Console.WriteLine($"  - {paidCount} marked as PAID (past/current months)");
Console.WriteLine($"  - {pendingCount} marked as PENDING (future months)");
Console.ResetColor();

// Verify
cmd.CommandText = "SELECT COUNT(*) FROM StudentScholarshipStatuses;";
$verifyCmd = connection.CreateCommand(); $verifyCmd.CommandText = "SELECT COUNT(*) FROM StudentScholarshipStatuses;"; var finalCount = (long)$verifyCmd.ExecuteScalar();
Console.WriteLine($"\n[VERIFICATION] Total records in table: {finalCount}");

Console.WriteLine("\n========== FIX COMPLETE ==========");
Console.WriteLine("\nNext steps:");
Console.WriteLine("1. Stop the running application (Get-Process -Name IzolluVakfi | Stop-Process)");
Console.WriteLine("2. Restart the application (dotnet run)");
Console.WriteLine("3. Navigate to Student Details → Burs Bilgileri tab");
Console.WriteLine("4. Select the active term - months should now appear with green checks!\n");

