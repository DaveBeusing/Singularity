# Building

## Prerequisites

- Windows on x64 hardware
- .NET 10 SDK
- PowerShell for the repository scripts

The application targets `net10.0-windows`, Windows Forms, and `win-x64`. Its manifest requests administrator privileges at runtime. Building and testing do not normally require elevation, but starting the application displays a UAC prompt.

## Repository layout

- `src/Singularity` — the production WinForms project
- `tests/Singularity.Tests` — deterministic unit tests
- `scripts` — build and repository validation scripts
- `docs` — architecture, development, and qualification documentation
- `.github/workflows` — repository CI workflows
- `Singularity.slnx` — repository-level solution
- `Directory.Build.props` — shared compiler settings
- `Directory.Packages.props` — central package versions

## Canonical validation

Run the repository validation script from the repository root:

```powershell
./scripts/build.ps1
```

Release is the default configuration. The script performs the canonical local validation sequence:

1. restore `Singularity.slnx`;
2. build the solution with warnings treated as errors;
3. run the deterministic test suite without rebuilding.

Any failed `dotnet` stage terminates the script with a non-zero process result.

Use Debug explicitly when needed:

```powershell
./scripts/build.ps1 -Configuration Debug
```

The `-Run` switch is opt-in and launches the application only after restore, build, and tests have succeeded:

```powershell
./scripts/build.ps1 -Run
```

CI never passes `-Run` and therefore never launches WinForms.

## Equivalent commands

The Release validation sequence can also be run directly:

```powershell
dotnet restore Singularity.slnx
dotnet build Singularity.slnx --configuration Release --no-restore
dotnet test Singularity.slnx --configuration Release --no-build
```

## Run

```powershell
dotnet run --project src/Singularity/Singularity.csproj
```

Because the application manifest requests elevation, launching it may require an elevated terminal depending on the host.

## Publish

Create the configured Release publication from the repository root:

```powershell
dotnet publish src/Singularity/Singularity.csproj --configuration Release --no-build
```

Run the canonical validation first when using `--no-build`.

Project metadata configures a self-contained, compressed, single-file `win-x64` publish and includes native libraries for extraction. Output is written beneath `src/Singularity/bin/Release/net10.0-windows/win-x64/publish/` unless an output path is supplied.

## Continuous integration

`.github/workflows/windows-validation.yml` runs on Windows for:

- pull requests targeting `master`;
- pushes to `master`.

The workflow installs the .NET 10 SDK, runs `./scripts/build.ps1 -Configuration Release`, then performs a Release publish smoke check. The smoke check uses the project's configured `win-x64`, self-contained, single-file settings and fails unless a non-empty `Singularity.exe` is produced.

Ordinary CI intentionally does not require administrator elevation, hardware access, NVML devices, Direct3D workload execution, or an interactive desktop. Authenticode signing, timestamping, and release publication remain separate release concerns and require no signing credentials in normal CI.

## Versioning

`Version` in `src/Singularity/Singularity.csproj` is the single source of truth. The UI and report exports read the generated assembly informational version at runtime, so a version change requires updating only that project property.
