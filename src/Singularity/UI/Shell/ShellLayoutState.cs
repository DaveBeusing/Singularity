// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Shell;

public sealed record ShellLayoutState(
	bool SidebarVisible,
	bool InspectorVisible,
	bool ToolPanelVisible)
{
	public static ShellLayoutState Default { get; } = new(
		SidebarVisible: true,
		InspectorVisible: false,
		ToolPanelVisible: false);

	public ShellLayoutState WithSidebar(bool visible) =>
		this with { SidebarVisible = visible };

	public ShellLayoutState WithInspector(bool visible) =>
		this with { InspectorVisible = visible };

	public ShellLayoutState WithToolPanel(bool visible) =>
		this with { ToolPanelVisible = visible };
}
