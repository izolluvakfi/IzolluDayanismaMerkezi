using Microsoft.Data.Sqlite;

var sourceDb = @"D:\K8s\CRM_V2_blazor\IzolluCRM\IzolluDayanismaMerkezi\DB's\izolluvakfi.db";
var targetDb = @"D:\K8s\CRM_V2_blazor\IzolluDayanismaMerkezi_APP\izolluvakfi.db";

foreach (var db in new[] { ("SOURCE", sourceDb), ("TARGET", targetDb) })
{
    Console.WriteLine($"\n=== Processing {db.Item1}: {db.Item2} ===\n");
    
    using var conn = new SqliteConnection($"Data Source={db.Item2}");
    conn.Open();
    
    // Update IsMalatyaUniversity for Malatya Turgut Özal and İnönü Universities
    var updateCmd = conn.CreateCommand();
    updateCmd.CommandText = @"
        UPDATE Students 
        SET IsMalatyaUniversity = 1 
        WHERE (Universite LIKE '%Turgut%' OR Universite LIKE '%Özal%' OR Universite LIKE '%Ozal%' 
               OR Universite LIKE '%İnönü%' OR Universite LIKE '%Inonu%' OR Universite LIKE '%Inönü%')
        AND (IsMalatyaUniversity = 0 OR IsMalatyaUniversity IS NULL)";
    var updated = updateCmd.ExecuteNonQuery();
    Console.WriteLine($"  Updated {updated} students to IsMalatyaUniversity = 1");
    
    // Show current state
    var checkCmd = conn.CreateCommand();
    checkCmd.CommandText = @"
        SELECT Universite, COUNT(*) as Count, MAX(IsMalatyaUniversity) as IsMalatya
        FROM Students 
        WHERE Universite LIKE '%Turgut%' OR Universite LIKE '%Özal%' OR Universite LIKE '%Ozal%' 
              OR Universite LIKE '%İnönü%' OR Universite LIKE '%Inonu%' OR Universite LIKE '%Inönü%'
        GROUP BY Universite";
    using var reader = checkCmd.ExecuteReader();
    Console.WriteLine("\n  Malatya Universities in DB:");
    while (reader.Read())
    {
        Console.WriteLine($"    {reader.GetString(0)}: {reader.GetInt32(1)} students, IsMalatya={reader.GetInt32(2)}");
    }
}

Console.WriteLine("\n\nDone!");
