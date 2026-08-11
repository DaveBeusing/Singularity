# Testing

The `tests/Singularity.Tests` xUnit project references the production application project and is part of `Singularity.slnx`.

Run all tests from the repository root:

```powershell
dotnet test
```

The current unit suite exercises deterministic behavior in:

- qualification-plan construction;
- manual and automated application-coordinator workflows;
- qualification-session lifecycle and telemetry statistics;
- bounded, newest-first qualification history;
- CPU, memory, and GPU validation thresholds and GPU warm-up behavior;
- report generation and invalid-session rejection;
- streaming minimum, average, maximum, sample count, and non-finite filtering;
- workload status states.

The tests do not launch WinForms and do not require administrator privileges. They intentionally avoid NVML, real GPU workloads, Direct3D, LibreHardwareMonitor sensors, WMI inventory, and other hardware-dependent paths.

Those integrations require manual validation on representative Windows hardware. Useful checks include inventory accuracy, missing-sensor behavior, NVIDIA GPU enumeration, workload start/stop behavior, UAC launch, and both report export dialogs.
