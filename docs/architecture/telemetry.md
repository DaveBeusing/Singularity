# Telemetry

`SystemMonitor` owns the runtime telemetry lifecycle. Constructing it creates the CPU and GPU providers, the synchronized cache, and a background `TelemetryScheduler`. Disposing it stops the scheduler and releases the process and provider resources.

## Sampling schedule

The scheduler wakes at 100 ms resolution and independently dispatches three sampling tiers:

| Tier | Interval | Values |
| --- | ---: | --- |
| Fast | 500 ms | System CPU load, Singularity process CPU and memory, physical-memory usage, GPU load and VRAM |
| Medium | 1 s | GPU temperature and power |
| Slow | 2 s | CPU temperature |

Sampling exceptions are written to debug output and do not terminate the scheduler.

## Providers

System CPU load comes from the Windows `GetSystemTimes` API. Process utilization comes from `Process.TotalProcessorTime`, and physical-memory figures come from `GlobalMemoryStatusEx`.

`LibreHardwareCpuTelemetryProvider` opens LibreHardwareMonitor with CPU, mainboard, and controller support. It recursively updates the hardware tree and prefers package, maximum-core, or vendor temperature sensors before falling back to any CPU-related temperature sensor. Missing or failed readings are represented as unavailable telemetry.

`NvmlGpuTelemetryProvider` loads NVIDIA Management Library (NVML), enumerates NVIDIA devices, and keeps one snapshot per device. Fast reads obtain utilization and memory; medium reads obtain temperature and optional power. Missing NVML, unsupported entry points, initialization failures, and individual read failures produce unavailable states rather than crashing the application. Non-NVIDIA GPU telemetry is not implemented.

## Cache and consumers

`TelemetryCache` protects the mutable snapshot with a lock. Writers update it inside the lock; readers receive a copy, so UI and validation code cannot mutate cached state.

The WinForms timer calls `SystemMonitor.GetSnapshot()` every 500 ms. The resulting cached snapshot updates Overview and is passed to `QualificationWorkspaceController`. The Qualification workspace and its Tool Panel render that same snapshot; they do not start additional polling. During an active workload the coordinator also adds it to `QualificationSession` statistics and passes it to `WorkloadValidator`.



## Inventory versus telemetry lifecycle

Platform inventory and telemetry are intentionally separate lifecycles.

`PlatformInventoryState` owns the stable hardware inventory used by Overview, Platform, and report export. Inventory discovery runs once after the main window is shown and thereafter only through explicit user refresh. A refresh is performed away from the WinForms UI thread, concurrent refresh requests are rejected, and the last valid inventory is retained if a later refresh fails.

`SystemMonitor` owns continuously sampled telemetry. Its scheduler and cache continue running while Overview or Platform is visible and while the user navigates between workspaces. UI refreshes read cached telemetry snapshots and never trigger full WMI or hardware inventory enumeration.

Unavailable CPU temperature and GPU telemetry remain explicit states in the UI rather than being replaced with synthetic zero values.
