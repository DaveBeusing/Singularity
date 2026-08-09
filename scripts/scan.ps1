# scan.ps1
# Erstellt repo_tree.txt und repo_scan.txt für ein C#/.NET Repository

param(
	[string]$RootPath = "",
    [string]$TreeOutput = "repo_tree.txt",
    [string]$ScanOutput = "repo_scan.txt"
)

$RepoRoot = if ([string]::IsNullOrWhiteSpace($RootPath)) {
	Split-Path -Parent $PSScriptRoot
} else {
	$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)
}
$TreeOutputPath = Join-Path $RepoRoot $TreeOutput
$ScanOutputPath = Join-Path $RepoRoot $ScanOutput

Push-Location $RepoRoot

Write-Host "Erzeuge Repository-Struktur..."
cmd /c "tree /f > `"$TreeOutputPath`""

Write-Host "Scanne C# Projektdateien..."

Get-ChildItem -Recurse -Include *.cs, *.csproj |
    Where-Object {
        $_.FullName -notmatch "\\bin\\|\\obj\\"
    } |
    Sort-Object FullName |
    ForEach-Object {
        "`n===== $($_.FullName) ====="
        Get-Content $_.FullName -Raw
    } | Set-Content $ScanOutputPath -Encoding UTF8

Pop-Location

Write-Host "Fertig."
Write-Host "Erstellt:"
Write-Host " - $TreeOutputPath"
Write-Host " - $ScanOutputPath"
