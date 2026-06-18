# build.ps1

param(
    [string]$RootPath = ".",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$Run
)

Set-Location $RootPath

Write-Host ""
Write-Host "=== Restore ===" -ForegroundColor Cyan

dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet restore fehlgeschlagen."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan

dotnet build `
    --configuration $Configuration `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build fehlgeschlagen."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Build erfolgreich." -ForegroundColor Green

if ($Run) {

    Write-Host ""
    Write-Host "=== Run ===" -ForegroundColor Cyan

    dotnet run `
        --configuration $Configuration `
        --no-build

    exit $LASTEXITCODE
}