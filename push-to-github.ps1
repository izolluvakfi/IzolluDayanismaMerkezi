# ============================================================
# IzolluDayanismaMerkezi - GitHub Push Scripti
# Calistirma: PowerShell'de  .\push-to-github.ps1
# ============================================================

Set-Location $PSScriptRoot

Write-Host "=== GitHub Push Basliyor ===" -ForegroundColor Cyan

# 1. Index lock varsa kaldir
if (Test-Path ".git\index.lock") {
    Write-Host "[1/5] index.lock kaldirilıyor..." -ForegroundColor Yellow
    Remove-Item ".git\index.lock" -Force
    Write-Host "      Lock kaldirildi." -ForegroundColor Green
} else {
    Write-Host "[1/5] index.lock yok, temiz." -ForegroundColor Green
}

# 2. DB dosyalarini tracking'den cikar
Write-Host "[2/5] DB dosyalari tracking'den cikariliyor..." -ForegroundColor Yellow
$filesToUntrack = @(
    "IzolluCRM/IzolluDayanismaMerkezi/izolluvakfi.db.backup_before_meetings",
    "IzolluDayanismaMerkezi_APP/izolluvakfi.db",
    "IzolluDayanismaMerkezi_APP/izolluvakfi.db-shm",
    "IzolluDayanismaMerkezi_APP/izolluvakfi.db-wal"
)

foreach ($file in $filesToUntrack) {
    $result = git ls-files --cached $file
    if ($result) {
        git rm --cached $file 2>$null
        Write-Host "      Kaldirildi: $file" -ForegroundColor Green
    }
}

# 3. IzolluDayanismaMerkezi_APP (derlenmiş binary'ler) tracking'den cikar
Write-Host "[3/5] Deploy binary klasoru tracking'den cikariliyor..." -ForegroundColor Yellow
$appFiles = git ls-files --cached "IzolluDayanismaMerkezi_APP/"
if ($appFiles) {
    git rm --cached -r "IzolluDayanismaMerkezi_APP/" 2>$null
    Write-Host "      IzolluDayanismaMerkezi_APP/ tracking'den cikarildi (kaynak kod kalmaya devam eder)." -ForegroundColor Green
}

# .rar dosyasini da cikar (buyuk binary arsiv)
$rarFile = git ls-files --cached "IzolluDayanismaMerkezi_APP.rar"
if ($rarFile) {
    git rm --cached "IzolluDayanismaMerkezi_APP.rar" 2>$null
    Write-Host "      IzolluDayanismaMerkezi_APP.rar kaldirildi." -ForegroundColor Green
}

# 4. Tum degisiklikleri ekle
Write-Host "[4/5] Degisiklikler stage'e ekleniyor..." -ForegroundColor Yellow
git add .
$status = git status --short
Write-Host "      Stage'deki dosya sayisi: $(($status | Measure-Object).Count)" -ForegroundColor Green

# 5. Commit ve push
Write-Host "[5/5] Commit ve push yapiliyor..." -ForegroundColor Yellow
$commitMsg = "Latest changes - $(Get-Date -Format 'yyyy-MM-dd HH:mm') - DB/binary excluded"
git commit -m $commitMsg

Write-Host "      origin/master'a push ediliyor..." -ForegroundColor Yellow
git push origin master

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "BASARILI! Repo: https://github.com/kaanbozbek/IzolluDayanismaMerkezi" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "HATA: Push basarisiz. Token/credential kontrolu yapiniz." -ForegroundColor Red
    Write-Host "Ipucu: git push origin master --verbose" -ForegroundColor Yellow
}
