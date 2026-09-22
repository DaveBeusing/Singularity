# UI Shell

Singularity uses a responsive editor-style Windows Forms shell while retaining the existing WinForms application technology and product identity.

## Top-level regions

The shell is composed under `UI/Shell` and provides stable regions for:

- the application title, active workspace context, and version;
- a left activity bar for primary workspace navigation;
- a contextual sidebar;
- the central workspace;
- an optional inspector;
- a collapsible tool panel;
- the global application status bar.

The canonical workspaces are Overview, Platform, Qualification, Results, Reports, and Settings. Navigation state is owned by `NavigationService`; activity-bar controls only reflect that state.

## Layout behavior

`ApplicationShell` uses nested WinForms `SplitContainer` controls and docked child controls. The activity bar has a fixed compact width, while the sidebar, workspace, inspector, and tool panel participate in resizable layout.

The sidebar is visible by default. The inspector and tool panel are optional and collapsed by default. Their visibility is represented by the immutable `ShellLayoutState`, which lets layout behavior be tested without creating WinForms handles.

The shell preserves a minimum central workspace size when optional regions are expanded. At narrow supported widths, sidebar space may contract so the workspace and inspector remain usable.

The main window is resizable, supports maximize and restore, and uses DPI autoscaling. Existing domain surfaces remain scrollable while their individual layouts are migrated incrementally.

## Workspace ownership

`WorkspaceHost` registers workspace controls once and switches visibility without reconstructing the control tree.

Overview, Platform, Qualification, Results, and Reports are persistent domain workspaces. Settings currently uses a technical placeholder surface until its domain migration.

The previous Workloads subview navigation is no longer part of the shell architecture. Qualification controls, current results, and reporting/history are separate top-level workspaces.

This is important for telemetry, qualification, and inventory rendering: timer-driven telemetry updates target existing controls; workspace navigation does not reconstruct the shell or trigger platform enumeration.

## Contextual regions

The sidebar is generated from the active `WorkspaceDefinition`. Context selection is maintained by `NavigationService`, along with a small workspace-scoped selection value for future inspector and contextual-action consumption.

Workspace definitions also declare whether inspector and tool-panel activation is supported. `ApplicationShell` can host workspace-specific content in those regions without introducing a docking framework. Platform uses the inspector for selection-driven metadata from memory modules, GPUs, and storage devices. With no detailed device selection, the inspector presents an explicit empty state.

## Commands and keyboard access

Global and contextual actions are routed through the lightweight `CommandRouter`. Commands expose enabled state from current application conditions, and existing qualification/report buttons are bound to those commands.

The activity bar and contextual sidebar use focusable buttons with standard Enter/Space activation. The shell also supports Ctrl+1 through Ctrl+6 for workspace navigation and focused shortcuts for optional shell regions.

See [Navigation and command model](navigation-command-model.md) for ownership, command state, prepared sidebar contexts, and keyboard mappings.

## Design system

The canonical Singularity palette remains defined by `Theme`:

- Background: `#121620`
- Panel: `#1C2230`
- PanelLight: `#262E40`
- Accent: `#FFC000`
- Success: `#50C878`
- Danger: `#DC4646`
- TextMain: `#F0F4F8`
- TextMuted: `#96A0AF`

Semantic shell roles such as application background, sidebar, workspace, status bar, hover, pressed, selected, focus, primary action, warning, and failure reference this canonical palette.

`ThemeMetrics` centralizes shell measurements such as activity-bar width, default sidebar width, inspector width, tool-panel height, status-bar height, spacing, control height, and minimum workspace/window dimensions.

Reusable shell controls live under `UI/Controls`, including command, activity, tool, status-indicator, metric-tile, property/value, selectable-device, and separator controls. Accent yellow is used selectively for active indicators, focus/primary action states, and warnings rather than large full-surface navigation selections.

## High-DPI and performance rules

The shell uses WinForms DPI autoscaling and docking/splitting rather than global absolute coordinates. Hardware discovery and telemetry acquisition remain outside paint and layout paths.

Hardware inventory remains a startup or explicit-refresh concern owned by `PlatformInventoryState`. Initial discovery begins after the main window is shown, and explicit refresh is scheduled away from the UI thread. The previous valid inventory remains visible when a refresh fails. Telemetry refreshes use `SystemMonitor` and its cache independently of inventory and update existing controls only. Expensive hardware enumeration must not be introduced into shell resize, repaint, navigation, or timer-rendering paths.

## Migration boundary

Overview and Platform are complete domain workspaces on the shell. Settings remains placeholder content and does not represent completed domain functionality.

The change does not redesign qualification business rules, telemetry sampling, workload execution, validation rules, reporting contracts, or hardware-provider behavior.
