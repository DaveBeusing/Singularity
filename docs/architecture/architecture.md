# Architecture

Singularity is a single-project Windows Forms application. The repository separates application source, tests, scripts, and documentation without splitting the runtime into additional assemblies.

## Source areas

- `Core/Qualification` defines automated plans, steps, progress, and the qualification runner.
- `Core/Reporting` collects session statistics and creates JSON or standalone HTML reports.
- `Core/Validation` owns profiles, live workload validation, sessions, and in-memory history.
- `Core/Workloads` starts and stops the CPU, memory, and GPU stress workers.
- `Application` coordinates manual and automated qualification workflows and report export without depending on WinForms.
- `Hardware` inventories the operating system, processor, mainboard, memory, storage, and NVIDIA GPUs. It contains WMI helpers, value decoders, and NVML interop.
- `Monitoring/Models` defines telemetry snapshots, `Monitoring/Providers` reads CPU and NVIDIA GPU sensors, and `Monitoring/Runtime` schedules sampling into a synchronized cache.
- `UI` contains the main form, views, sections, controls, layout constants, and theme definitions.

The main dependencies flow inward from the UI to the core services and platform adapters:

```text
WinForms UI
    |-- Hardware inventory providers
    |-- SystemMonitor
    `-- Application services

QualificationCoordinator --> QualificationRunner + validation + sessions + reporting
QualificationRunner      --> WorkloadManager
WorkloadValidator  --> WorkloadStatus + SystemSnapshot
ReportGenerator    --> QualificationSession + ValidationResult
Report exporters   --> QualificationReport + HardwareInventory
```

Core validation and reporting do not depend on WinForms. Hardware providers and monitoring providers contain the Windows- and device-specific integrations, while monitoring runtime code owns scheduling and caching.

## Application flow

At startup, `Program` initializes WinForms high-DPI defaults and opens `MainForm`. The form builds the platform and workload views. `HardwareView` obtains the static machine inventory, while `SystemMonitor` begins background sampling.

The UI timer reads the latest cached snapshot every 500 milliseconds and passes it to `QualificationCoordinator`. While a workload is active, the coordinator records the snapshot in the current session and asks `WorkloadValidator` for CPU, memory, and GPU results. Manual runs stop on user request. Automated runs delegate step transitions to `QualificationRunner`.

When a session finishes, the coordinator freezes its final status and telemetry statistics, adds a record to the in-memory history, and uses `QualificationReportGenerator` to create an exportable report. `ReportExportService` combines that report with the current hardware inventory through the JSON or HTML exporter. `MainForm` retains only UI-specific dialogs, messages, navigation, and rendering.

Related documents:

- [Telemetry](telemetry.md)
- [Qualification runner](../qualification/qualification-runner.md)
- [Validation](../qualification/validation.md)
- [Reporting](../qualification/reporting.md)
