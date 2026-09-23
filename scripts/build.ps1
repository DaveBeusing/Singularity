# build.ps1

param(
	[string]$RootPath = "",
	[ValidateSet("Debug", "Release")]
	[string]$Configuration = "Release",
	[switch]$Run
)

$ErrorActionPreference = "Stop"

$RepoRoot = if ([string]::IsNullOrWhiteSpace($RootPath)) {
	Split-Path -Parent $PSScriptRoot
} else {
	$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)
}

$ProjectPath = Join-Path $RepoRoot "src\Singularity\Singularity.csproj"
$SolutionPath = Join-Path $RepoRoot "Singularity.slnx"

function Invoke-DotNetStage {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Name,
		[Parameter(Mandatory = $true)]
		[string[]]$Arguments
	)

	Write-Host ""
	Write-Host "=== $Name ===" -ForegroundColor Cyan

	& dotnet @Arguments
	$ExitCode = $LASTEXITCODE

	if ($ExitCode -ne 0) {
		throw "$Name failed with exit code $ExitCode."
	}
}

Push-Location $RepoRoot

try {
	Invoke-DotNetStage -Name "Restore" -Arguments @(
		"restore",
		$SolutionPath
	)

	Invoke-DotNetStage -Name "Build ($Configuration)" -Arguments @(
		"build",
		$SolutionPath,
		"--configuration", $Configuration,
		"--no-restore"
	)

	Invoke-DotNetStage -Name "Test ($Configuration)" -Arguments @(
		"test",
		$SolutionPath,
		"--configuration", $Configuration,
		"--no-build",
		"--no-restore"
	)

	Write-Host ""
	Write-Host "Validation succeeded." -ForegroundColor Green

	if ($Run) {
		Invoke-DotNetStage -Name "Run ($Configuration)" -Arguments @(
			"run",
			"--project", $ProjectPath,
			"--configuration", $Configuration,
			"--no-build"
		)
	}
}
finally {
	Pop-Location
}
