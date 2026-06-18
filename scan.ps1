# scan-repo.ps1
# Erstellt repo_tree.txt und repo_scan.txt für ein C#/.NET Repository

param(
    [string]$RootPath = ".",
    [string]$TreeOutput = "repo_tree.txt",
    [string]$ScanOutput = "repo_scan.txt"
)

Set-Location $RootPath

Write-Host "Erzeuge Repository-Struktur..."
cmd /c "tree /f > `"$TreeOutput`""

Write-Host "Scanne C# Projektdateien..."

Get-ChildItem -Recurse -Include *.cs, *.csproj |
    Where-Object {
        $_.FullName -notmatch "\\bin\\|\\obj\\"
    } |
    Sort-Object FullName |
    ForEach-Object {
        "`n===== $($_.FullName) ====="
        Get-Content $_.FullName -Raw
    } | Set-Content $ScanOutput -Encoding UTF8

Write-Host "Fertig."
Write-Host "Erstellt:"
Write-Host " - $TreeOutput"
Write-Host " - $ScanOutput"