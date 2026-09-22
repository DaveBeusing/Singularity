# UI Shell

Singularity uses a responsive editor-style Windows Forms shell while retaining the existing WinForms application technology and product identity.

## Top-level regions

The shell is composed under `UI/Shell` and provides stable regions for:

- the application title and version;
- a left activity bar for primary navigation;
- a contextual sidebar;
- the central workspace;
- an optional inspector;
- a collapsible tool panel;
- the global application status bar.

`MainForm` owns top-level application interaction and binds qualification state to the shell. It no longer owns fixed tab geometry or fixed window dimensions.

## Layout behavior

`ApplicationShell` uses nested WinForms `SplitContainer` controls and docked child controls. The activity bar has a fixed compact width, while the sidebar, workspace, inspector, and tool panel participate in resizable layout.

The sidebar is visible by default. The inspector and tool panel are optional and collapsed by default. Their visibility is represented by the immutable `ShellLayoutState`, which lets layout behavior be tested without creating WinForms handles.

The shell preserves a minimum central workspace size when optional regions are expanded. At narrow supported widths, sidebar space may contract so the workspace and inspector remain usable.

The main window is resizable, supports maximize and restore, and uses DPI autoscaling. Legacy workspaces remain scrollable while their individual layouts are migrated incrementally.

## Workspace ownership

`WorkspaceHost` registers workspace controls once and switches visibility without reconstructing the control tree. The current Platform and Workloads views are created once by `MainForm` and hosted through this mechanism.

This is important for telemetry and qualification rendering: timer-driven state updates target the existing controls and do not recreate the shell or workspace hierarchy.

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

Reusable shell controls live under `UI/Controls`, including command, activity, tool, status-indicator, and separator controls. Accent yellow is used selectively for active indicators, focus/primary action states, and warnings rather than large full-surface navigation selections.

## High-DPI and performance rules

The shell uses WinForms DPI autoscaling and docking/splitting rather than global absolute coordinates. Hardware discovery and telemetry acquisition must remain outside paint and layout paths.

Hardware inventory remains a startup or explicit-refresh concern. Telemetry refreshes update existing controls and cached application state only. Expensive hardware enumeration must not be introduced into shell resize, repaint, navigation, or timer-rendering paths.

## Migration boundary

The shell is the foundation for later workspace-specific UI migrations. Platform and Workloads currently retain their established internal layouts so this change does not redesign qualification, telemetry, workload, validation, reporting, or hardware-provider behavior.
