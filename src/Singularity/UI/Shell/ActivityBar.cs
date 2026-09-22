// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Controls;

namespace Singularity.UI.Shell;

public sealed class ActivityBar : Panel
{
	private readonly ActivityButton platformButton = new();
	private readonly ActivityButton workloadsButton = new();
	private readonly ToolButton sidebarButton = new();
	private readonly ToolButton inspectorButton = new();
	private readonly ToolButton toolsButton = new();
	private readonly ToolTip toolTip = new();

	public ActivityBar()
	{
		Dock = DockStyle.Left;
		Width = ThemeMetrics.ActivityBarWidth;
		BackColor = Theme.ActivityBar;

		FlowLayoutPanel navigation = new()
		{
			Dock = DockStyle.Top,
			Height = 112,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			Padding = new Padding(6, 8, 6, 0),
			BackColor = Theme.ActivityBar
		};

		ConfigureActivityButton(platformButton, "P", "Platform", ShellSection.Platform);
		ConfigureActivityButton(workloadsButton, "W", "Workloads", ShellSection.Workloads);

		navigation.Controls.Add(platformButton);
		navigation.Controls.Add(workloadsButton);

		FlowLayoutPanel tools = new()
		{
			Dock = DockStyle.Bottom,
			Height = 148,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			Padding = new Padding(6, 4, 6, 8),
			BackColor = Theme.ActivityBar
		};

		ConfigureToolButton(sidebarButton, "S", "Toggle sidebar", () => ToggleSidebarRequested?.Invoke());
		ConfigureToolButton(inspectorButton, "I", "Toggle inspector", () => ToggleInspectorRequested?.Invoke());
		ConfigureToolButton(toolsButton, "T", "Toggle tools", () => ToggleToolPanelRequested?.Invoke());

		tools.Controls.Add(sidebarButton);
		tools.Controls.Add(inspectorButton);
		tools.Controls.Add(toolsButton);

		Controls.Add(tools);
		Controls.Add(navigation);
	}

	public event Action<ShellSection>? NavigationRequested;
	public event Action? ToggleSidebarRequested;
	public event Action? ToggleInspectorRequested;
	public event Action? ToggleToolPanelRequested;

	public void SetActive(ShellSection section)
	{
		platformButton.Selected = section == ShellSection.Platform;
		workloadsButton.Selected = section == ShellSection.Workloads;
	}

	private void ConfigureActivityButton(
		ActivityButton button,
		string text,
		string accessibleName,
		ShellSection section)
	{
		button.Text = text;
		button.AccessibleName = accessibleName;
		button.BackColor = Theme.ActivityBar;
		button.Margin = new Padding(0, 0, 0, 4);
		button.Click += (_, _) => NavigationRequested?.Invoke(section);
		toolTip.SetToolTip(button, accessibleName);
	}

	private void ConfigureToolButton(
		ToolButton button,
		string text,
		string accessibleName,
		Action action)
	{
		button.Text = text;
		button.AccessibleName = accessibleName;
		button.Margin = new Padding(0, 0, 0, 4);
		button.Click += (_, _) => action();
		toolTip.SetToolTip(button, accessibleName);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			toolTip.Dispose();

		base.Dispose(disposing);
	}
}
