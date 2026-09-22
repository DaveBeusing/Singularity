# Navigation and Command Model

Singularity uses a state-driven workspace and command model on top of the editor-style WinForms shell.

## Canonical workspaces

The top-level workspace catalog is defined in `UI/Navigation/WorkspaceCatalog.cs` and contains, in order:

- Overview
- Platform
- Qualification
- Results
- Reports
- Settings

`NavigationService` owns the active workspace, the active contextual sidebar item, and the current scoped selection. Activity-bar and sidebar controls reflect that state; they are not the source of truth.

Unknown workspace identifiers and unknown contextual items fail without mutating the current navigation state.

## Workspace lifecycle

`ApplicationShell` hosts workspace controls through `WorkspaceHost`. A workspace is registered once and reused when navigation changes, which avoids reconstructing hardware, telemetry, qualification, result, and reporting controls during navigation.

The current domain mapping is:

- Overview -> `OverviewView`
- Platform -> `PlatformView`
- Qualification -> `QualificationView`
- Results -> `ResultsView`
- Reports -> `ReportsView`
- Settings -> `SettingsView`

Settings is intentionally narrow: it controls sidebar, inspector, and tool-panel visibility for the current application session and can reset the shell layout to defaults. It does not create a general preferences subsystem and does not persist values across application restarts.

The previous nested `RUN / RESULTS / HISTORY` navigation is removed. Qualification controls, current results, and report/history surfaces now belong to separate top-level workspaces.

## Contextual sidebar

Each `WorkspaceDefinition` supplies contextual navigation items. The current prepared contexts are:

- Platform: System, CPU, Memory, GPU, Storage
- Qualification: Profile, Workloads, Session
- Results: Latest, Validation, Statistics
- Reports: History, Report, Export
- Settings: Layout

Overview retains its summary/status contexts. Results and Reports use their context selection to update inspector or focus behavior without reconstructing workspace controls.

Context selection is owned by `NavigationService`. Platform context switches the central explorer between System, CPU, Memory, GPU, and Storage. Device selection publishes a workspace-scoped `WorkspaceSelection`, while `PlatformInspectorView` renders detailed metadata for selectable memory, GPU, and storage devices.

## Inspector and tool panel

Workspace definitions declare whether inspector and tool-panel activation is supported.

`ApplicationShell` exposes explicit registration points for optional inspector and tool-panel content. Unsupported regions are collapsed visually without discarding the user's session layout preference. Qualification registers a contextual inspector for Profile, Workloads, and Session and a Tool Panel surface for cached telemetry and automated-run progress. Results and Reports register inspectors for evidence and selected-report details.

## Command routing

Global and contextual actions are represented by `CommandId` and routed through `CommandRouter`.

Current commands include:

- start qualification;
- automated qualification;
- stop qualification;
- export JSON;
- export HTML;
- explicit hardware inventory refresh;
- toggle sidebar;
- toggle inspector;
- toggle tool panel.

Each command exposes current enabled state through its registered `CanExecute` function. Qualification controls and report export buttons are bound through `ButtonCommandBinding`, so invalid actions are disabled instead of relying on error dialogs. Report export is enabled only when the selected history entry contains report evidence and platform inventory is available.

The explicit hardware inventory refresh uses the application-lifetime `PlatformInventoryState`, runs outside the UI thread, rejects duplicate concurrent refreshes, and preserves the previous valid inventory on failure. Normal telemetry, navigation, layout, and paint paths do not perform hardware enumeration.

## Keyboard behavior

Activity-bar and sidebar entries are real focusable WinForms buttons and support standard Enter/Space activation.

The shell additionally provides:

- Ctrl+1 through Ctrl+6 -> Overview through Settings
- Ctrl+B -> toggle sidebar
- Ctrl+Alt+I -> toggle inspector where supported
- Ctrl+J -> toggle tool panel where supported
- Ctrl+R -> refresh hardware inventory while Platform is active and refresh is enabled

Focus indication remains provided by the shared command-button styling. The custom Singularity checkbox is also keyboard-focusable and supports Space/Enter activation for Qualification and Settings workflows.

## Ownership

`MainForm` composes application services, registers persistent workspaces, and renders cross-workspace results and global status. Qualification command registration, qualification button bindings, configuration state, and current Qualification presentation are owned by `QualificationWorkspaceController` and `QualificationWorkspaceState`.

`ApplicationShell` owns shell navigation, contextual sidebar rendering, optional-region visibility, shortcuts, workspace activation, and shell command integration.

Core qualification, workload, validation, reporting, monitoring, and hardware-provider behavior remains outside the navigation layer.
