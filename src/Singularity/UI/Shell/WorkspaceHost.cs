// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Shell;

public sealed class WorkspaceHost : Panel
{
	private readonly Dictionary<ShellSection, Control> workspaces = [];

	public WorkspaceHost()
	{
		Dock = DockStyle.Fill;
		AutoScroll = true;
		BackColor = Theme.Workspace;
		Padding = new Padding(ThemeMetrics.Spacing);
	}

	public ShellSection? ActiveSection { get; private set; }

	public void Register(ShellSection section, Control content)
	{
		ArgumentNullException.ThrowIfNull(content);

		if (workspaces.ContainsKey(section))
			throw new InvalidOperationException($"Workspace '{section}' is already registered.");

		content.Visible = false;
		content.Location = new Point(ThemeMetrics.Spacing, ThemeMetrics.Spacing);
		content.Anchor = AnchorStyles.Top | AnchorStyles.Left;
		workspaces.Add(section, content);
		Controls.Add(content);
	}

	public void Activate(ShellSection section)
	{
		if (!workspaces.TryGetValue(section, out Control? activeContent))
			throw new InvalidOperationException($"Workspace '{section}' has not been registered.");

		foreach ((ShellSection key, Control content) in workspaces)
			content.Visible = key == section;

		ActiveSection = section;
		activeContent.BringToFront();
		AutoScrollPosition = Point.Empty;
		AutoScrollMinSize = new Size(
			activeContent.Width + (ThemeMetrics.Spacing * 2),
			activeContent.Height + (ThemeMetrics.Spacing * 2));
	}
}
