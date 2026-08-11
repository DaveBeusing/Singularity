# Singularity

Singularity is a Windows hardware qualification tool for collecting system inventory, observing live telemetry, applying controlled workloads, validating results, and exporting qualification reports.

![Platform](https://img.shields.io/badge/.NET-10.0-blue)
![Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- Collects inventory for Windows, mainboard and BIOS, CPU, memory modules, storage devices, and NVIDIA GPUs.
- Monitors system and process CPU usage, process and physical memory, CPU temperature, and NVIDIA GPU load, temperature, power, and VRAM usage.
- Uses tiered telemetry scheduling and caching to balance responsiveness with hardware-query cost.
- Provides independently configurable CPU, memory, and Direct3D 12 GPU workloads, including combined runs.
- Includes Quick, Standard, and Burn-in qualification profiles.
- Evaluates CPU, memory, and GPU checks as PASS, WARNING, or FAIL.
- Records qualification sessions with validation results and minimum, average, and maximum telemetry statistics.
- Keeps the ten most recent sessions in an in-memory history.
- Runs automated qualification plans with dedicated and combined workload steps.
- Exports qualification results as JSON or standalone HTML reports, including profile, validation, statistics, hardware inventory, and application version.

## Repository Structure

```text
.
├─ docs/
│  ├─ architecture/
│  ├─ development/
│  └─ qualification/
├─ scripts/
├─ src/
│  └─ Singularity/
│     ├─ Application/
│     ├─ Core/
│     ├─ Hardware/
│     ├─ Monitoring/
│     ├─ Properties/
│     └─ UI/
├─ tests/
│  └─ Singularity.Tests/
├─ .editorconfig
├─ .gitignore
├─ Directory.Build.props
├─ Directory.Packages.props
├─ LICENSE.md
├─ README.md
└─ Singularity.slnx
```

- `src/Singularity` contains the WinForms application, domain models, platform integrations, monitoring services, and UI.
- `tests/Singularity.Tests` contains deterministic unit tests that do not require physical hardware, elevation, or an interactive desktop.
- `docs` contains architecture, development, validation, qualification, and reporting documentation.
- `scripts` contains local build and repository inspection helpers.

## Architecture

Singularity uses a layered qualification pipeline:

```text
Platform Inventory
        ↓
Telemetry
        ↓
Workloads
        ↓
Validation
        ↓
Qualification
        ↓
Reporting
```

The application composition root creates platform services, the qualification coordinator, and the WinForms UI. See the [architecture overview](docs/architecture/architecture.md) for component responsibilities and data flow.

## Requirements

- Windows 10 or Windows 11 on x64 hardware.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for source builds.
- Administrator approval when the application starts; the executable requests elevation through UAC.
- A Direct3D 12-capable GPU and runtime supporting feature level 11_0 and shader model 6.0 for the GPU workload.
- An NVIDIA driver exposing NVML for NVIDIA GPU inventory and telemetry. The remaining application features continue to work when NVML is unavailable.

## Build

Restore dependencies and build the solution from the repository root:

```powershell
dotnet restore
dotnet build
```

For the repository build workflow, including validation checks, run:

```powershell
./scripts/build.ps1
```

## Test

Run the deterministic test suite from the repository root:

```powershell
dotnet test
```

Hardware access, elevation behavior, and interactive UI operation require validation on a representative Windows system and are intentionally outside the unit-test suite.

## Run

Start the application from the repository root:

```powershell
dotnet run --project src/Singularity/Singularity.csproj
```

Windows displays a UAC prompt because Singularity requires administrator privileges for hardware monitoring and qualification operations.

## Publish

Create the configured Release publication from the repository root:

```powershell
dotnet publish src/Singularity/Singularity.csproj --configuration Release
```

The project publishes for `win-x64` as a self-contained, compressed single-file application. Output is written beneath `src/Singularity/bin/Release/net10.0-windows/win-x64/publish` unless an output path is supplied.

## Documentation

- [Architecture overview](docs/architecture/architecture.md)
- [Telemetry design](docs/architecture/telemetry.md)
- [Building](docs/development/building.md)
- [Testing](docs/development/testing.md)
- [Qualification runner](docs/qualification/qualification-runner.md)
- [Validation](docs/qualification/validation.md)
- [Reporting](docs/qualification/reporting.md)

## Roadmap

Remaining opportunities include persisting qualification history across application restarts, extending GPU inventory and telemetry beyond NVIDIA/NVML, and adding hardware-integration coverage across representative systems.

## License
Copyright (c) 2026 David Beusing

Singularity is available under the [MIT License](LICENSE.md).
