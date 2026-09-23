# Singularity

Singularity is a Windows hardware qualification tool for collecting system inventory, observing live telemetry, applying controlled workloads, validating results, and exporting qualification reports.

![Platform](https://img.shields.io/badge/.NET-10.0-blue)
![Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- Collects inventory for Windows, mainboard and BIOS, CPU, memory modules, storage devices, and vendor-neutral Windows GPUs through DXGI/Direct3D 12, with NVIDIA-specific NVML enrichment when available.
- Monitors system and process CPU usage, process and physical memory, CPU temperature, and NVIDIA GPU load, temperature, power, and VRAM usage.
- Uses tiered telemetry scheduling and caching to balance responsiveness with hardware-query cost.
- Provides independently configurable CPU, memory, and Direct3D 12 GPU workloads, including combined runs.
- Includes Quick, Standard, and Burn-in qualification profiles.
- Evaluates CPU, memory, and GPU checks as PASS, WARNING, or FAIL.
- Records qualification sessions with validation results and minimum, average, and maximum telemetry statistics.
- Keeps the ten most recent sessions in the live runtime history and persists up to 100 completed qualification records per Windows user with frozen profile, run mode, result, telemetry statistics, per-device GPU evidence, and report evidence where available.
- Runs automated qualification plans with dedicated and combined workload steps.
- Exports the selected qualification report as JSON or standalone HTML, including profile, validation, statistics, hardware inventory, and application version.
- Uses a resizable editor-style WinForms shell with persistent Overview, Platform, Qualification, Results, Reports, and Settings workspaces, contextual navigation, optional inspector/tool regions, and a global status bar.
- Provides a dedicated Qualification workspace with state-aware commands, persistent-in-session configuration, current-session state, automated progress, failure feedback, contextual inspection, and live cached telemetry in the Tool Panel.
- Presents the latest completed qualification evidence in Results and bounded selectable history with report preview/export in Reports.
- Provides Settings for session-only sidebar, inspector, and tool-panel layout visibility plus local qualification-archive status and explicit clear/delete control.
- Caches platform inventory outside the UI lifecycle, supports explicit non-blocking refresh, and provides component-level Platform navigation with selection-driven device inspection.

## Repository Structure

```text
.
├─ .github/
│  └─ workflows/
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
- `scripts` contains local build and repository validation helpers.
- `.github/workflows` contains the Windows pull-request and `master` validation workflow.

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
- NVIDIA telemetry enrichment requires an NVIDIA driver exposing NVML. Baseline GPU inventory and Direct3D 12 qualification discovery do not depend on NVML.

## Build and Validate

Run the canonical repository validation from the repository root:

```powershell
./scripts/build.ps1
```

The script restores `Singularity.slnx`, builds Release with warnings treated as errors, and runs the deterministic test suite. Use `-Configuration Debug` when required. `-Run` is explicit and launches the application only after validation succeeds.

Equivalent Release commands are:

```powershell
dotnet restore Singularity.slnx
dotnet build Singularity.slnx --configuration Release --no-restore
dotnet test Singularity.slnx --configuration Release --no-build
```

## Continuous Integration

`.github/workflows/windows-validation.yml` runs automatically for pull requests targeting `master` and pushes to `master`.

CI runs the canonical Release validation on a Windows runner with the .NET 10 SDK, then performs a publish smoke check for the configured self-contained, compressed, single-file `win-x64` application. The workflow fails unless `Singularity.exe` is produced and is non-empty.

Hardware-dependent qualification, administrator/UAC behavior, interactive WinForms automation, Authenticode signing, timestamping, and release publication are intentionally outside ordinary CI.

## Test

Run only the deterministic tests after a successful Release build with:

```powershell
dotnet test Singularity.slnx --configuration Release --no-build
```

Hardware access, elevation behavior, and interactive UI operation require validation on a representative Windows system and are intentionally outside the unit-test suite.

## Run

Start the application from the repository root:

```powershell
dotnet run --project src/Singularity/Singularity.csproj
```

Windows displays a UAC prompt because Singularity requires administrator privileges for hardware monitoring and qualification operations.

## Publish

After a successful Release validation, create the configured publication from the repository root:

```powershell
dotnet publish src/Singularity/Singularity.csproj --configuration Release --no-build
```

The project publishes for `win-x64` as a self-contained, compressed single-file application. Output is written beneath `src/Singularity/bin/Release/net10.0-windows/win-x64/publish` unless an output path is supplied.

## Documentation

- [Architecture overview](docs/architecture/architecture.md)
- [UI shell](docs/architecture/ui-shell.md)
- [Results, Reports, and Settings workspaces](docs/architecture/results-reports-settings.md)
- [Navigation and command model](docs/architecture/navigation-command-model.md)
- [Telemetry design](docs/architecture/telemetry.md)
- [GPU inventory](docs/architecture/gpu-inventory.md)
- [Building](docs/development/building.md)
- [Testing](docs/development/testing.md)
- [Qualification workspace](docs/qualification/qualification-workspace.md)
- [Qualification runner](docs/qualification/qualification-runner.md)
- [Validation](docs/qualification/validation.md)
- [Reporting](docs/qualification/reporting.md)\n- [Qualification archive](docs/qualification/qualification-archive.md)

## Roadmap

Remaining opportunities include extending vendor-specific telemetry beyond NVIDIA/NVML, defining migration behavior when a future qualification-archive schema requires it, completing the trusted release-signing pipeline, and adding hardware-integration coverage across representative GPU and system configurations.

## License

Copyright (c) 2026 David Beusing

Singularity is available under the [MIT License](LICENSE.md).
