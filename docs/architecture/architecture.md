# Architecture

Singularity is a single-project Windows Forms application. The repository separates application source, tests, scripts, and documentation without splitting the runtime into additional assemblies.

## Source areas

- `Core/Qualification` defines automated plans, steps, progress, and the qualification runner.
- `Core/Reporting` collects session statistics and creates JSON or standalone HTML reports.
- `Core/Validation` owns profiles, live workload validation, sessions, and in-memory history.
- `Core/Workloads` starts and stops the CPU, memory, and GPU stress workers.
- `Hardware` inventories the operating system, processor, mainboard, memory, storage, and NVIDIA GPUs. It contains WMI helpers, value decoders, and NVML interop.
- `Monitoring` samples CPU, memory, process, and NVIDIA GPU telemetry into a synchronized cache.
- `UI` contains the main form, views, sections, controls, layout constants, and theme definitions.

The main dependencies flow inward from the UI to the core services and platform adapters:

```text
WinForms UI
    |-- Hardware inventory providers
    |-- SystemMonitor
    |-- WorkloadManager
    `-- Qualification / validation / reporting

QualificationRunner --> WorkloadManager
WorkloadValidator  --> WorkloadStatus + SystemSnapshot
ReportGenerator    --> QualificationSession + ValidationResult
Report exporters   --> QualificationReport + HardwareInventory
```

Core validation and reporting do not depend on WinForms. Hardware and monitoring code contain the Windows- and device-specific integrations.

## Application flow

At startup, `Program` initializes WinForms high-DPI defaults and opens `MainForm`. The form builds the platform and workload views. `HardwareView` obtains the static machine inventory, while `SystemMonitor` begins background sampling.

The UI timer reads the latest cached snapshot every 500 milliseconds. While a workload is active, the form records the snapshot in the current session and asks `WorkloadValidator` for CPU, memory, and GPU results. Manual runs stop on user request. Automated runs delegate step transitions to `QualificationRunner`.

When a session finishes, its final status and telemetry statistics are frozen, a record is added to the in-memory history, and `QualificationReportGenerator` creates an exportable report. JSON and HTML exporters combine that report with the current hardware inventory.

Related documents:

- [Telemetry](telemetry.md)
- [Qualification runner](../qualification/qualification-runner.md)
- [Validation](../qualification/validation.md)
- [Reporting](../qualification/reporting.md)

