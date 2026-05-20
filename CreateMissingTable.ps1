$dbPath = "D:\K8s\CRM_V2_blazor\IzolluCRM\IzolluDayanismaMerkezi\izolluvakfi.db"

Add-Type -Path "C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.11\Microsoft.Data.Sqlite.dll"

$connectionString = "Data Source=$dbPath"
$connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)

try {
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = @"
CREATE TABLE IF NOT EXISTS StudentScholarshipStatuses (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    TermId INTEGER NOT NULL,
    IsActive INTEGER NOT NULL,
    StartDate TEXT NOT NULL,
    EndDate TEXT NULL,
    MonthlyAmount REAL NOT NULL,
    Notes TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (TermId) REFERENCES Terms(Id)
);
"@
    
    $command.ExecuteNonQuery() | Out-Null
    Write-Host "Table StudentScholarshipStatuses created successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
finally {
    $connection.Close()
}
