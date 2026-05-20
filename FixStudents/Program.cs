using Microsoft.Data.Sqlite;
using System;

var dbPath = @"D:\K8s\CRM_V2_blazor\IzolluDayanismaMerkezi_APP\izolluvakfi.db";

var connectionString = $"Data Source={dbPath}";
using var connection = new SqliteConnection(connectionString);
connection.Open();

// Get count before
var countCmd = connection.CreateCommand();
countCmd.CommandText = "SELECT COUNT(*) FROM Students WHERE MezunMu = 1";
var beforeCount = Convert.ToInt32(countCmd.ExecuteScalar());
Console.WriteLine($"Before: {beforeCount} students marked as graduated");

// Update all students that were marked as graduated to be active scholarship students
var updateCmd = connection.CreateCommand();
updateCmd.CommandText = @"
    UPDATE Students 
    SET MezunMu = 0, 
        AktifBursMu = 1,
        MezuniyetTarihi = NULL
    WHERE MezunMu = 1";

var updated = updateCmd.ExecuteNonQuery();
Console.WriteLine($"Updated {updated} students: MezunMu=0, AktifBursMu=1");

// Verify
countCmd.CommandText = "SELECT COUNT(*) FROM Students WHERE MezunMu = 1";
var afterGraduated = Convert.ToInt32(countCmd.ExecuteScalar());

countCmd.CommandText = "SELECT COUNT(*) FROM Students WHERE AktifBursMu = 1";
var afterActive = Convert.ToInt32(countCmd.ExecuteScalar());

Console.WriteLine($"\nAfter update:");
Console.WriteLine($"  Graduated students: {afterGraduated}");
Console.WriteLine($"  Active scholarship students: {afterActive}");
