// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Navigation;

namespace Singularity.UI.Shell;

public sealed class WorkspaceHost : Panel
{
	private readonly Dictionary<WorkspaceId, Control> workspaces = [];

	public WorkspaceHost()
	{
		Dock = DockStyle.Fill;
		AutoScroll = true;
		BackColor = Theme.Workspace;
		Padding = new Padding(ThemeMetrics.Spacing);
	}

	public WorkspaceId? ActiveWorkspace { get; private set; }

	public void Register(WorkspaceId workspace, Control content)
	{
		ArgumentNullException.ThrowIfNull(content);

		if (workspaces.ContainsKey(workspace))
			throw new InvalidOperationException($"Workspace '{workspace}' is already registered.");

		content.Visible = false;
		content.Location = new Point(ThemeMetrics.Spacing, ThemeMetrics.Spacing);
		content.Anchor = AnchorStyles.Top | AnchorStyles.Left;
		workspaces.Add(workspace, content);
		Controls.Add(content);
	}

	public bool TryActivate(WorkspaceId workspace)
	{
		if (!workspaces.TryGetValue(workspace, out Control? activeContent))
			return false;

		foreach ((WorkspaceId key, Control content) in workspaces)
			content.Visible = key == workspace;

		ActiveWorkspace = workspace;
		activeContent.BringToFront();
		AutoScrollPosition = Point.Empty;
		AutoScrollMinSize = new Size(
			activeContent.Width + (ThemeMetrics.Spacing * 2),
			activeContent.Height + (ThemeMetrics.Spacing * 2));
		return true;
	}
}
