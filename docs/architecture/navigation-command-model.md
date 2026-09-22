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

- Platform -> `HardwareView`
- Qualification -> `QualificationView`
- Results -> `ResultsView`
- Reports -> `ReportsView`

Overview and Settings intentionally use placeholder surfaces until their domain content is migrated. The placeholder surfaces are infrastructure only and must not be interpreted as completed domain implementations.

The previous nested `RUN / RESULTS / HISTORY` navigation is removed. Qualification controls, current results, and report/history surfaces now belong to separate top-level workspaces.

## Contextual sidebar

Each `WorkspaceDefinition` supplies contextual navigation items. The current prepared contexts are:

- Platform: System, CPU, Memory, GPU, Storage
- Qualification: Profile, Workloads, Session
- Results: Latest, Validation, Statistics
- Reports: History, Preview, Export

Overview and Settings also provide small placeholder context sets.

Context selection is owned by `NavigationService`. It also publishes a small `WorkspaceSelection` value that can be consumed by future inspector content without introducing a generic event bus.

## Inspector and tool panel

Workspace definitions declare whether inspector and tool-panel activation is supported.

`ApplicationShell` exposes explicit registration points for optional inspector and tool-panel content. Unsupported regions are collapsed when navigation moves to a workspace that does not opt into them.

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

Each command exposes current enabled state through its registered `CanExecute` function. Qualification controls and report export buttons are bound through `ButtonCommandBinding`, so invalid actions are disabled instead of relying on error dialogs.

The explicit hardware inventory refresh runs outside the UI thread. Normal telemetry and layout refresh paths do not perform hardware enumeration.

## Keyboard behavior

Activity-bar and sidebar entries are real focusable WinForms buttons and support standard Enter/Space activation.

The shell additionally provides:

- Ctrl+1 through Ctrl+6 -> Overview through Settings
- Ctrl+B -> toggle sidebar
- Ctrl+Alt+I -> toggle inspector where supported
- Ctrl+J -> toggle tool panel where supported
- Ctrl+R -> refresh hardware inventory while Platform is active and refresh is enabled

Focus indication remains provided by the shared command-button styling.

## Ownership

`MainForm` composes application services, registers the persistent workspaces, registers domain commands, binds existing domain buttons to commands, and renders qualification state.

`ApplicationShell` owns shell navigation, contextual sidebar rendering, optional-region visibility, shortcuts, workspace activation, and shell command integration.

Core qualification, workload, validation, reporting, monitoring, and hardware-provider behavior remains outside the navigation layer.
