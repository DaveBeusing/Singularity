# Architecture

Singularity is a single-project Windows Forms application. The repository separates application source, tests, scripts, and documentation without splitting the runtime into additional assemblies.

## Source areas

- `Core/Qualification` defines automated plans, steps, progress, and the qualification runner.
- `Core/Reporting` collects session statistics and creates JSON or standalone HTML reports.
- `Core/Validation` owns profiles, live workload validation, sessions, and in-memory history.
- `Core/Workloads` starts and stops the CPU, memory, and GPU stress workers.
- `Application` coordinates manual and automated qualification workflows, report export, and application-lifetime platform inventory state without depending on WinForms. `Application/Commands` contains the lightweight command-routing model used by the shell and domain controls.
- `Hardware` inventories the operating system, processor, mainboard, memory, storage, and NVIDIA GPUs. It contains WMI helpers, value decoders, and NVML interop.
- `Monitoring/Models` defines telemetry snapshots, `Monitoring/Providers` reads CPU and NVIDIA GPU sensors, and `Monitoring/Runtime` schedules sampling into a synchronized cache.
- `UI/Navigation` owns canonical workspace definitions, active navigation state, contextual sidebar selection, and scoped workspace selection.
- `UI/Shell` owns the editor-style activity bar, sidebar, workspace host, inspector, tool panel, status bar, keyboard routing, and button-to-command binding.
- `UI/Views` contains persistent Overview, Platform, Qualification, Results, Reports, and Settings workspaces. Results/Reports presentation state remains separate from the validation/reporting domain, while Settings is limited to session-only shell layout preferences.

The main dependencies flow inward from the UI to the core services and platform adapters:

```text
WinForms UI
    |-- NavigationService
    |-- CommandRouter
    |-- PlatformInventoryState
    |-- SystemMonitor
    `-- Application services

PlatformInventoryState    --> HardwareProvider

QualificationCoordinator --> QualificationRunner + validation + sessions + reporting
QualificationRunner      --> WorkloadManager
WorkloadValidator        --> WorkloadStatus + SystemSnapshot
ReportGenerator          --> QualificationSession + ValidationResult
Report exporters         --> QualificationReport + HardwareInventory
```

Core validation and reporting do not depend on WinForms. Hardware providers and monitoring providers contain the Windows- and device-specific integrations, while monitoring runtime code owns scheduling and caching.

## Application flow

At startup, `Program` initializes WinForms high-DPI defaults and opens `MainForm`. The form composes `NavigationService`, `CommandRouter`, and `ApplicationShell`, creates the persistent domain workspaces once, registers them with `WorkspaceHost`, and binds existing domain buttons to application commands.

The canonical top-level workspaces are Overview, Platform, Qualification, Results, Reports, and Settings. Overview presents current readiness, platform identity, live CPU/RAM/GPU highlights, storage summary, and the latest qualification result. Platform is a component explorer for System, CPU, Memory, GPU, and Storage with selection-driven device inspection. Qualification hosts workload configuration, live telemetry, and run control. Results presents the latest completed qualification evidence. Reports presents bounded in-memory history, selected report review, and state-aware JSON/HTML export. Settings exposes only session-only shell layout preferences already supported by the shell.

`Program` creates one `HardwareProvider` and one application-lifetime `PlatformInventoryState`. After the main window is shown, `MainForm` starts the initial inventory discovery asynchronously. The resulting inventory is cached and shared by Overview, Platform, and report export. Explicit refresh is routed through the same state owner, rejects concurrent refreshes, runs discovery away from the UI thread, and preserves the last valid inventory if a refresh fails. Navigation and repainting never trigger hardware discovery. `SystemMonitor` independently begins background telemetry sampling.

The UI timer reads the latest cached snapshot every 500 milliseconds and passes it to `QualificationCoordinator`. While a workload is active, the coordinator records the snapshot in the current session and asks `WorkloadValidator` for CPU, memory, and GPU results. Manual runs stop on user request. Automated runs delegate step transitions to `QualificationRunner`.

When a session finishes, the coordinator freezes final status and telemetry statistics. The bounded in-memory history retains the session profile, execution mode, result, frozen statistics, and the generated `QualificationReport` when validation evidence is sufficient to create one. This allows Results to remain useful for failed sessions without inventing validation data, while Reports can select and export earlier report evidence. `ReportExportService` combines the selected report with the current hardware inventory through the existing JSON or HTML exporter.

`MainForm` remains the composition point for application interaction, dialogs, report export, binding qualification state into persistent workspace controls, and marshaling platform-inventory results into the UI. Device selection is mapped into workspace-scoped navigation selection and the Platform inspector. Navigation ownership, contextual sidebar state, workspace activation, optional shell regions, shortcuts, and shell-level command routing are owned by `ApplicationShell`, `NavigationService`, and `CommandRouter`.

Related documents:

- [UI shell](ui-shell.md)
- [Results, Reports, and Settings](results-reports-settings.md)
- [Navigation and command model](navigation-command-model.md)
- [Telemetry](telemetry.md)
- [Qualification runner](../qualification/qualification-runner.md)
- [Validation](../qualification/validation.md)
- [Reporting](../qualification/reporting.md)
