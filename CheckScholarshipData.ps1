$dbPath = "D:\K8s\CRM_V2_blazor\IzolluCRM\IzolluDayanismaMerkezi\izolluvakfi.db"

# Load .NET SQLite assembly
Add-Type -Path "C:\Users\$env:USERNAME\.nuget\packages\microsoft.data.sqlite.core\8.0.0\lib\net8.0\Microsoft.Data.Sqlite.dll" -ErrorAction Stop

$connection = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbPath")
$connection.Open()

Write-Host "`n========== DATABASE DIAGNOSIS ==========" -ForegroundColor Cyan

# 1. Check Terms
$cmd = $connection.CreateCommand()
$cmd.CommandText = "SELECT Id, DisplayName, Start, End, IsActive FROM Terms ORDER BY DisplayName DESC LIMIT 5;"
Write-Host "`n--- TERMS (Latest 5) ---" -ForegroundColor Yellow
$reader = $cmd.ExecuteReader()
$terms = @()
while ($reader.Read()) {
    $term = @{
        Id = $reader.GetInt32(0)
        DisplayName = $reader.GetString(1)
        Start = $reader.GetString(2)
        End = $reader.GetString(3)
        IsActive = $reader.GetInt32(4)
    }
    $terms += $term
    Write-Host "ID: $($term.Id) | Name: $($term.DisplayName) | Active: $($term.IsActive) | Start: $($term.Start)"
}
$reader.Close()

# 2. Check StudentScholarshipStatuses count
$cmd.CommandText = "SELECT COUNT(*) FROM StudentScholarshipStatuses;"
Write-Host "`n--- StudentScholarshipStatuses COUNT ---" -ForegroundColor Yellow
$count = $cmd.ExecuteScalar()
Write-Host "Total Records: $count"

# 3. Check sample records grouped by TermId
$cmd.CommandText = @"
SELECT TermId, COUNT(*) as Count, MIN(Year) as MinYear, MAX(Year) as MaxYear
FROM StudentScholarshipStatuses
GROUP BY TermId
ORDER BY TermId DESC;
"@
Write-Host "`n--- StudentScholarshipStatuses by TermId ---" -ForegroundColor Yellow
$reader = $cmd.ExecuteReader()
$hasRecords = $false
while ($reader.Read()) {
    $hasRecords = $true
    $termId = $reader.GetInt32(0)
    $count = $reader.GetInt32(1)
    $minYear = $reader.GetInt32(2)
    $maxYear = $reader.GetInt32(3)
    Write-Host "TermId: $termId | Count: $count | Year Range: $minYear - $maxYear"
}
$reader.Close()

if (-not $hasRecords) {
    Write-Host "NO SCHOLARSHIP STATUS RECORDS FOUND!" -ForegroundColor Red
}

# 4. Check if there are orphaned records (TermId pointing to non-existent Terms)
$cmd.CommandText = @"
SELECT DISTINCT sss.TermId 
FROM StudentScholarshipStatuses sss
LEFT JOIN Terms t ON sss.TermId = t.Id
WHERE t.Id IS NULL;
"@
Write-Host "`n--- ORPHANED RECORDS (TermId with no matching Term) ---" -ForegroundColor Yellow
$reader = $cmd.ExecuteReader()
$orphans = @()
while ($reader.Read()) {
    if (-not $reader.IsDBNull(0)) {
        $orphans += $reader.GetInt32(0)
    }
}
$reader.Close()

if ($orphans.Count -gt 0) {
    Write-Host "FOUND ORPHANED RECORDS with TermIds: $($orphans -join ', ')" -ForegroundColor Red
} else {
    Write-Host "No orphaned records found." -ForegroundColor Green
}

# 5. Check Students count
$cmd.CommandText = "SELECT COUNT(*) FROM Students;"
$studentCount = $cmd.ExecuteScalar()
Write-Host "`n--- Students COUNT: $studentCount ---" -ForegroundColor Yellow

$connection.Close()

Write-Host "`n========== DIAGNOSIS COMPLETE ==========`n" -ForegroundColor Cyan
