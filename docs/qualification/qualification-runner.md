# Qualification Runner

Automated qualification is represented by three types:

- `QualificationPlan` names a run, holds its ordered steps, and controls whether validation failure stops the run.
- `QualificationStep` pairs a name and duration with one `WorkloadOptions` configuration.
- `QualificationRunner` starts workloads, advances steps, publishes progress, and finishes or cancels the run.

## Plan construction

`QualificationPlan.CreateStandard` creates a dedicated step for every enabled CPU, memory, or GPU workload. If more than one workload is selected, it appends a `COMBINED` step with all selected workloads enabled. At least one workload must be selected.

The selected profile's recommended duration is divided evenly across all generated steps. Workload intensity values—CPU thread count, memory allocation, and GPU load target—are copied into each applicable step.

## Runner lifecycle

Starting a runner stores the plan, selects its first step, resets prior workload failure, and starts the `WorkloadManager`. Starting an already-active runner or supplying an empty plan is rejected.

The UI calls `Update` with the latest validation result. The runner:

1. fails immediately if the workload manager reports a failure;
2. fails on unsuccessful validation when `StopOnFailure` is enabled;
3. otherwise waits for the current step duration;
4. stops the current workload and starts the next step;
5. completes after the final step.

Cancellation stops the workload and marks the run cancelled. Reset is allowed only while no run is active. `QualificationProgress` exposes the current step, elapsed and target durations, step counts, state, and a bounded overall percentage.

`QualificationCoordinator` coordinates the runner with session tracking, telemetry validation, and final report generation. `MainForm` passes user commands and snapshots to the coordinator and renders its exposed state.
