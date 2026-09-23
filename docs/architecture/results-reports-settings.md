# Results, Reports, and Settings Workspaces

The final editor-style UI migration separates qualification execution, completed evidence, history/report review, and shell preferences into persistent top-level workspaces.

## Results

Results derives from the newest completed record in the coordinator's bounded evidence list. At runtime this list combines the current ten-entry `QualificationHistory` with the locally persisted qualification archive loaded at startup. It presents:

- overall PASS/WARNING/FAIL state with a text label and semantic color;
- profile and manual/automated execution mode;
- start, finish, and duration metadata;
- CPU, memory, and derived GPU validation states;
- one evidence card per selected GPU with stable identity and per-device validation;
- frozen CPU/system-memory statistics plus per-GPU load, temperature, power, VRAM, and telemetry-gap statistics where available.

No completed record produces an explicit empty state. Missing telemetry or validation evidence remains unavailable/unknown and is never converted into a numeric zero.

Results does not own a second qualification model. It maps the existing history/report evidence into presentation state through `ResultsWorkspaceState`.

## Reports

Reports consumes the same archive-backed evidence list. The left history surface selects a record; the central surface and inspector render that selection. A new completed record becomes the selected entry automatically.

A history record retains frozen per-device GPU evidence and the generated `QualificationReport` when one exists. The report preview renders each selected GPU separately. JSON and HTML export are routed through the existing `ReportExportService` and act on the selected report. Export is disabled unless both report evidence and current platform inventory are available.

`QualificationHistory` remains capped at ten entries for the live runtime model. `QualificationArchiveService` persists up to 100 completed records by default under `%LOCALAPPDATA%\\Singularity\\qualification-archive.json`, so Results and Reports can be rehydrated after restart.

## Settings

Settings is not a general configuration framework. It exposes only shell capabilities already supported by `ApplicationShell`:

- context sidebar visibility;
- inspector visibility preference;
- tool-panel visibility preference;
- reset layout to defaults.

These layout preferences apply only to the current application session. Settings separately shows qualification-archive loading/ready/failure state, stored-record count, local storage path, and an explicitly confirmed clear action.

Inspector and tool-panel preferences are retained when navigating through a workspace that does not support the requested region. The unsupported region is collapsed visually and reappears when the user enters a compatible workspace.

## Status and tool surfaces

The global status bar presents the active workspace, qualification state, active/session profile, and compact CPU/RAM/GPU telemetry. Qualification remains the only workspace that currently registers bottom Tool Panel content, using real cached telemetry and qualification progress rather than synthetic diagnostics data.

## Responsive and accessibility behavior

Results uses docked/table-based layouts and an explicit scrollable workspace. Reports uses a resizable split layout whose minimum panel sizes remain below the shell minimum workspace width. History controls update their widths as the available client area changes.

Primary actions are focusable. Activity/sidebar buttons and command buttons expose visible keyboard focus. The shared Singularity checkbox supports Tab focus plus Space/Enter activation. Status is communicated with visible PASS/WARNING/FAIL text in addition to color.

High-DPI behavior remains owned by the .NET 10 WinForms DPI configuration and the shell's docked/split layout. Interactive qualification is still required at 100%, 125%, 150%, and 200% scaling on representative Windows systems.
