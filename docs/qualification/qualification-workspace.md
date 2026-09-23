# Qualification Workspace

The Qualification workspace is the editor-style control surface for configuring and running manual or automated platform qualification.

It presents current application and domain state; it does not own workload execution, qualification orchestration, telemetry polling, validation, or report generation.

## Responsibilities

The workspace provides:

- qualification profile selection;
- CPU, memory, and GPU workload configuration;
- state-aware Start, Automated, and Stop / Cancel commands;
- explicit workload and session lifecycle state;
- automated qualification step and percentage progress;
- a compact current-session summary;
- persistent warning and failure feedback;
- a contextual inspector for Profile, Workloads, and Session;
- live telemetry and qualification progress in the bottom Tool Panel;
- navigation to Results after a session has completed or failed.

QualificationWorkspaceState is application-owned presentation state. It retains the selected configuration and current presentation mode independently from WinForms control lifetime.

QualificationWorkspaceController connects that state to QualificationCoordinator, the command router, navigation, the workspace view, inspector, and tool panel.

## Domain ownership

The existing qualification stack remains authoritative:

- WorkloadManager owns workload execution and workload lifecycle state.
- QualificationRunner owns automated step execution and progress.
- QualificationCoordinator owns the active qualification session, validation integration, history, and report generation.
- SystemMonitor owns telemetry acquisition and caching.
- QualificationWorkspaceState maps those states for presentation.

The workspace never creates workload workers, duplicates qualification steps, or performs sensor polling.

## Configuration

The user can configure:

- CPU workload enabled state and thread count;
- memory workload enabled state and allocation in GB;
- GPU workload enabled state, one-or-more stable GPU device selections, and target load percentage;
- Quick, Standard, or BurnIn qualification profile.

At least one workload must be selected before a qualification can start. A single GPU with a stable identity is selected automatically. Systems with multiple selectable GPUs expose a checked multi-selection list. Stable identifiers, not list positions, define the selection. Selections survive inventory reordering. If one or more selected devices disappear after refresh, the remaining valid identities are preserved, an explicit warning is shown, and the selection must be reviewed before qualification can start.

Configuration controls are disabled while a qualification session is active. The selected configuration remains application state when the user navigates to another workspace and is reapplied when Qualification is shown again.

## Commands

### Start

Starts a manual qualification using the currently selected configuration and profile.

Enabled only when:

- at least one workload is selected;
- no qualification session is running;
- no automated qualification is running;
- workload state is Stopped or Failed.

### Automated

Starts the existing standard automated qualification plan for the selected workloads and profile.

Its availability follows the same conditions as Start.

### Stop / Cancel

Stops a manual qualification or cancels an automated qualification.

It is enabled only while the current session is active and the underlying workload or automated runner is active.

Rejected or invalid operations are rendered as persistent workspace feedback rather than relying on modal dialogs.

## State presentation

The workspace explicitly represents:

- Stopped;
- Starting;
- Running;
- Stopping;
- Failed.

Automated qualification additionally exposes:

- current step name;
- step number and total step count;
- bounded overall percentage;
- completed, cancelled, and failed states.

The current-session summary contains:

- profile;
- mode;
- start time;
- elapsed time;
- workload status;
- overall state.

Detailed validation evidence and statistics remain in Results.

## Telemetry and Tool Panel

The bottom Tool Panel renders cached telemetry supplied by SystemMonitor through the application's existing 500 ms UI update path.

It shows:

- CPU load and CPU temperature or explicit temperature-unavailable status;
- system-memory utilization;
- selected-GPU load/temperature summary, availability count, optional power for a single selected GPU, or explicit per-selection telemetry-unavailable status;
- automated qualification progress or current qualification state.

The Qualification workspace does not create timers, hardware enumeration, or additional sensor polling.

Required telemetry that is unavailable is shown explicitly. Missing telemetry is never replaced with a synthetic numeric zero.

## Inspector

The contextual sidebar contains Profile, Workloads, and Session.

The Qualification inspector renders details for the active context without changing domain state:

- Profile: active profile duration and validation thresholds;
- Workloads: selected workloads, all selected stable GPU identities, and configured targets;
- Session: mode, profile, timestamps, progress, and current lifecycle state.

## Navigation behavior

Qualification is registered once in the persistent WorkspaceHost.

Navigating away while a run is active:

- does not stop or restart workloads;
- does not recreate qualification orchestration;
- does not discard configuration;
- does not stop telemetry acquisition.

Returning to Qualification reconstructs the visible workspace, inspector, tool panel, and command state from QualificationWorkspaceState plus current coordinator and telemetry state.

## Failure and recovery

Persistent feedback covers:

- workload failure;
- automated qualification failure;
- automated qualification cancellation;
- required telemetry unavailable;
- rejected start or stop operations.

Manual workload failure finalizes the active qualification session as failed so a failed workload cannot leave an orphaned running session.

There are no hidden automatic retries after workload failure.

## Results boundary

Qualification contains only the current high-value session context.

After completion or failure, the user can navigate to Results for validation evidence and statistics. Report history and export remain in Reports.


## GPU selection behavior

Qualification only offers GPUs that expose a stable provider identity. Transient index-based NVML fallback identifiers are not accepted for explicit qualification targeting.

All selected identifiers are copied into `WorkloadOptions` and remain part of `WorkloadStatus` while the workload runs. GPU initialization must resolve every exact identity to its Windows graphics adapter. The selected adapters execute concurrently with independent worker/resource ownership. Resolution or execution failure is a workload failure and is shown through the normal qualification feedback path.

The same identifiers resolve live telemetry, validation, frozen session statistics, Results evidence, and report evidence. Another GPU's telemetry is never substituted when a selected device is missing, unavailable, removed, or reordered.
