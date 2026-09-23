# Testing

The `tests/Singularity.Tests` xUnit project references the production application project and is part of `Singularity.slnx`.

## Canonical test execution

Run the complete repository validation from the repository root:

```powershell
./scripts/build.ps1
```

The script restores, builds, and then runs the deterministic test suite. To execute the tests directly after a successful Release build:

```powershell
dotnet test Singularity.slnx --configuration Release --no-build
```

A failing deterministic test must return a non-zero result locally and causes the Windows validation workflow to fail.

## Deterministic coverage

The current unit suite exercises deterministic behavior in:

- qualification-plan construction;
- manual and automated application-coordinator workflows;
- qualification-session lifecycle and telemetry statistics;
- bounded, newest-first qualification history;
- CPU, memory, and per-device GPU validation thresholds, independent GPU warm-up/stability behavior, mixed outcomes, and telemetry gaps;
- multi-GPU selection preservation across inventory reordering and stale-device handling;
- per-device streaming telemetry aggregation and frozen qualification evidence;
- report generation, schema-2.0 JSON evidence, standalone HTML evidence, single-GPU compatibility, and invalid-session rejection;
- streaming minimum, average, maximum, sample count, and non-finite filtering;
- workload status states;
- qualification workspace command availability, progress, session, navigation, telemetry-unavailable, and failure-state mapping;
- Results empty/latest-evidence mapping, including failed-session telemetry without an exportable report;
- Reports selection and export-availability mapping;
- bounded history retention of profile, execution mode, telemetry, and report evidence;
- deterministic shell region visibility and reset state.

The tests do not launch WinForms and do not require administrator privileges. They intentionally avoid NVML, real GPU workloads, Direct3D, LibreHardwareMonitor sensors, WMI inventory, and other hardware-dependent paths.

## CI validation boundary

`.github/workflows/windows-validation.yml` runs the deterministic suite on a GitHub-hosted Windows runner for pull requests targeting `master` and pushes to `master`. After the tests pass, CI performs a Release publish smoke check and verifies that the configured single-file `Singularity.exe` exists and is non-empty.

CI intentionally does not validate hardware-dependent qualification, administrator/UAC behavior, interactive WinForms operation, or production Authenticode signing. Those concerns require representative Windows hardware or the separate release workflow.

## Manual Windows validation

Hardware integrations require manual validation on representative Windows hardware. For multi-GPU qualification, use a system with at least two supported NVIDIA adapters and verify that each selected stable identity maps to the intended Windows adapter, both selected adapters receive Direct3D 12 workload concurrently, deselected adapters are not exercised, cancellation stops every worker cleanly, one-device failure is surfaced as an overall failure, telemetry gaps remain attached to the correct identity, and exported JSON/HTML evidence matches the devices actually exercised. Also verify inventory accuracy, missing-sensor behavior, workload start/stop behavior, UAC launch, and both report export dialogs.

### UI shell validation

The deterministic test suite covers shell visibility-state transitions without creating WinForms handles. Interactive validation is still required for minimum-window resize behavior, 1080p/1440p/4K layouts, maximize/restore, sidebar and optional-region resizing, keyboard focus/activation, Results/Reports empty and selection states, Settings layout toggles, export permission failures, active qualification while navigating, clean shutdown, and Windows display scaling at 100%, 125%, 150%, and 200% where practical.
