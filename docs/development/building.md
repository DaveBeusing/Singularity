# Building

## Prerequisites

- Windows on x64 hardware
- .NET 10 SDK
- PowerShell for the repository scripts

The application targets `net10.0-windows`, Windows Forms, and `win-x64`. Its manifest requests administrator privileges at runtime. Building and testing do not normally require elevation, but starting the application displays a UAC prompt.

## Repository layout

- `src/Singularity` — the production WinForms project
- `tests/Singularity.Tests` — deterministic unit tests
- `scripts` — build and repository inspection scripts
- `docs` — architecture, development, and qualification documentation
- `Singularity.slnx` — repository-level solution
- `Directory.Build.props` — shared compiler settings
- `Directory.Packages.props` — central package versions

## Restore and build

Run from the repository root:

```powershell
dotnet restore
dotnet build
```

For a release build and optional launch, use:

```powershell
./scripts/build.ps1
./scripts/build.ps1 -Run
```

The script resolves the repository root from its own location, restores the solution, and builds Release by default. Pass `-Configuration Debug` when needed.

## Run

```powershell
dotnet run --project src/Singularity/Singularity.csproj
```

Because the application manifest requests elevation, launching it may require an elevated terminal depending on the host.

## Publish

```powershell
dotnet publish src/Singularity/Singularity.csproj --configuration Release
```

Project metadata configures a self-contained, compressed, single-file `win-x64` publish and includes native libraries for extraction. Output is written beneath `src/Singularity/bin/Release/net10.0-windows/win-x64/publish/` unless an output path is supplied.

