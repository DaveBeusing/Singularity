# build.ps1

param(
	[string]$RootPath = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$Run
)

$RepoRoot = if ([string]::IsNullOrWhiteSpace($RootPath)) {
	Split-Path -Parent $PSScriptRoot
} else {
	$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)
}
$ProjectPath = Join-Path $RepoRoot "src\Singularity\Singularity.csproj"

Push-Location $RepoRoot

Write-Host ""
Write-Host "=== Restore ===" -ForegroundColor Cyan

dotnet restore $ProjectPath

if ($LASTEXITCODE -ne 0) {
	Pop-Location
    Write-Error "dotnet restore fehlgeschlagen."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan

dotnet build `
	$ProjectPath `
    --configuration $Configuration `
    --no-restore

if ($LASTEXITCODE -ne 0) {
	Pop-Location
    Write-Error "dotnet build fehlgeschlagen."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Build erfolgreich." -ForegroundColor Green

if ($Run) {

    Write-Host ""
    Write-Host "=== Run ===" -ForegroundColor Cyan

    dotnet run `
		--project $ProjectPath `
        --configuration $Configuration `
        --no-build
	$RunExitCode = $LASTEXITCODE
	Pop-Location
	exit $RunExitCode
}

Pop-Location
