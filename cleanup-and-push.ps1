# ============================================================
# IzolluDayanismaMerkezi - Temizlik + Push Scripti
# Calistirma: PowerShell'i VS Code DISINDA ac, sonra:
#   cd D:\K8s\CRM_V2_blazor
#   .\cleanup-and-push.ps1
# ============================================================

Set-Location $PSScriptRoot

$TOKEN  = $env:GITHUB_TOKEN   # Komut satirindan: $env:GITHUB_TOKEN="<token>"; .\cleanup-and-push.ps1
if (-not $TOKEN) {
    $TOKEN = Read-Host "GitHub Personal Access Token gir"
}
$REMOTE = "https://izolluvakfi:$TOKEN@github.com/izolluvakfi/IzolluDayanismaMerkezi.git"

Write-Host "=== Temizlik + Push Basliyor ===" -ForegroundColor Cyan

# 1. Lock dosyasini kaldir
if (Test-Path ".git\index.lock") {
    Remove-Item ".git\index.lock" -Force
    Write-Host "[1] index.lock kaldirildi." -ForegroundColor Green
}

# 2. Bozuk index'i sifirla (VS Code'dan kaynaklanan corrupt index icin)
Write-Host "[2] Git index yenileniyor..." -ForegroundColor Yellow
Remove-Item ".git\index" -Force -ErrorAction SilentlyContinue
git reset HEAD 2>$null
if ($LASTEXITCODE -ne 0) {
    git checkout HEAD -- . 2>$null
}

# 3. Gereksiz klasor ve dosyalari tracking'den cikar
Write-Host "[3] Gereksiz dosyalar git'ten kaldiriliyor..." -ForegroundColor Yellow

$toRemove = @(
    "IzolluDayanismaMerkezi_APP_NEW",
    "IzolluDayanismaMerkezi_05012025_KaynakKod",
    "IzolluDayanismaMerkezi_05012025",
    "IzolluCRM/IzolluDayanismaMerkezi/DB's",
    "IzolluCRM/IzolluDayanismaMerkezi/publish",
    "IzolluCRM/IzolluDayanismaMerkezi/build_log.txt",
    "IzolluCRM/IzolluDayanismaMerkezi/build_output.txt",
    "IzolluCRM/IzolluDayanismaMerkezi/izolluvakfi.db.backup_20260104_215012",
    "IzolluCRM/IzolluDayanismaMerkezi/izolluvakfi.db.backup_before_meetings",
    "CLAUDE_CODE_SKILL.md",
    "MEZUN_OGR.xlsx",
    "AddIsMaxGradeReachedColumn.cs",
    "FixScholarshipTracking.cs",
    "CheckScholarshipData.ps1",
    "CreateMissingTable.ps1",
    "IzolluDayanismaMerkezi_05012025.rar",
    "IzolluDayanismaMerkezi_05012025_KaynakKod.rar",
    "bin",
    "obj"
)

foreach ($item in $toRemove) {
    $tracked = git ls-files --cached $item 2>$null
    if ($tracked) {
        git rm --cached -r $item 2>$null
        Write-Host "   Kaldirildi: $item" -ForegroundColor DarkGray
    }
}

# 4. .gitignore'a ek satirlar ekle (yoksa)
$gitignorePath = ".gitignore"
$additions = @(
    "IzolluDayanismaMerkezi_APP_NEW/",
    "IzolluDayanismaMerkezi_05012025/",
    "IzolluDayanismaMerkezi_05012025_KaynakKod/",
    "MEZUN_OGR.xlsx",
    "CLAUDE_CODE_SKILL.md",
    "*.rar",
    "build_log.txt",
    "build_output.txt"
)
$existing = Get-Content $gitignorePath -Raw
foreach ($line in $additions) {
    if ($existing -notmatch [regex]::Escape($line)) {
        Add-Content $gitignorePath "`n$line"
    }
}

# 5. Tum degisiklikleri stage'e al
Write-Host "[4] Stage'e ekleniyor..." -ForegroundColor Yellow
git add .
$count = (git status --short | Measure-Object).Count
Write-Host "    $count dosya staged." -ForegroundColor Green

# 6. Commit
Write-Host "[5] Commit yapiliyor..." -ForegroundColor Yellow
git commit -m "Clean repo: remove publish artifacts, add README with deployment guide"

# 7. Push (token ile)
Write-Host "[6] izolluvakfi/IzolluDayanismaMerkezi'ne push ediliyor..." -ForegroundColor Yellow
git push $REMOTE master

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "BASARILI!" -ForegroundColor Green
    Write-Host "Repo: https://github.com/izolluvakfi/IzolluDayanismaMerkezi" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "ONEMLI: Bu scripti sildikten sonra GitHub'dan token'i iptal edin:" -ForegroundColor Yellow
    Write-Host "  github.com -> izolluvakfi -> Settings -> Developer Settings -> Personal Access Tokens" -ForegroundColor Yellow
} else {
    Write-Host "HATA: Push basarisiz." -ForegroundColor Red
}
