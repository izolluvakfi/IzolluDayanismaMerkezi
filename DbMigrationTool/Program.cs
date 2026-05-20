using Microsoft.Data.Sqlite;

var connectionString = @"Data Source=D:\K8s\CRM_V2_blazor\IzolluCRM\IzolluDayanismaMerkezi\izolluvakfi.db";

using var connection = new SqliteConnection(connectionString);
connection.Open();

Console.WriteLine("Checking for IsMaxGradeReached column...");

// Check if column exists
var checkCmd = connection.CreateCommand();
checkCmd.CommandText = "PRAGMA table_info(Students);";
using var reader = checkCmd.ExecuteReader();

bool hasIsMaxGradeReached = false;
while (reader.Read())
{
    var columnName = reader.GetString(1);
    if (columnName == "IsMaxGradeReached")
    {
        hasIsMaxGradeReached = true;
        break;
    }
}
reader.Close();

if (!hasIsMaxGradeReached)
{
    Console.WriteLine("Adding IsMaxGradeReached column...");
    var alterCmd = connection.CreateCommand();
    alterCmd.CommandText = "ALTER TABLE Students ADD COLUMN IsMaxGradeReached INTEGER NOT NULL DEFAULT 0;";
    alterCmd.ExecuteNonQuery();
    Console.WriteLine(" IsMaxGradeReached column added successfully!");
}
else
{
    Console.WriteLine(" IsMaxGradeReached column already exists.");
}

// Update migration history
Console.WriteLine("\nUpdating migration history...");
var updateHistoryCmd = connection.CreateCommand();
updateHistoryCmd.CommandText = @"
    INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
    VALUES ('20260104210357_AddIsMaxGradeReachedFlag', '8.0.0');
";
updateHistoryCmd.ExecuteNonQuery();
Console.WriteLine(" Migration history updated!");

Console.WriteLine("\n Migration completed successfully!");
