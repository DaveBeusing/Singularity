// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application.Commands;
using Singularity.UI.Controls;
using Singularity.UI.Navigation;

namespace Singularity.UI.Shell;

public sealed class ActivityBar : Panel
{
	private readonly Dictionary<WorkspaceId, ActivityButton> navigationButtons = [];
	private readonly List<ButtonCommandBinding> commandBindings = [];
	private readonly ToolButton sidebarButton = new();
	private readonly ToolButton inspectorButton = new();
	private readonly ToolButton toolsButton = new();
	private readonly ToolTip toolTip = new();

	public ActivityBar(
		IReadOnlyList<WorkspaceDefinition> workspaces,
		CommandRouter commandRouter)
	{
		ArgumentNullException.ThrowIfNull(workspaces);
		ArgumentNullException.ThrowIfNull(commandRouter);

		Dock = DockStyle.Left;
		Width = ThemeMetrics.ActivityBarWidth;
		BackColor = Theme.ActivityBar;

		FlowLayoutPanel navigation = new()
		{
			Dock = DockStyle.Top,
			Height = Math.Max(112, 16 + workspaces.Count * (ThemeMetrics.ActivityButtonSize + 4)),
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			Padding = new Padding(6, 8, 6, 0),
			BackColor = Theme.ActivityBar
		};

		int tabIndex = 0;
		foreach (WorkspaceDefinition workspace in workspaces)
		{
			ActivityButton button = new();
			ConfigureActivityButton(button, workspace, tabIndex++);
			navigationButtons.Add(workspace.Id, button);
			navigation.Controls.Add(button);
		}

		FlowLayoutPanel tools = new()
		{
			Dock = DockStyle.Bottom,
			Height = 148,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			Padding = new Padding(6, 4, 6, 8),
			BackColor = Theme.ActivityBar
		};

		ConfigureToolButton(sidebarButton, "S", "Toggle sidebar", tabIndex++);
		ConfigureToolButton(inspectorButton, "I", "Toggle inspector", tabIndex++);
		ConfigureToolButton(toolsButton, "T", "Toggle tools", tabIndex);

		tools.Controls.Add(sidebarButton);
		tools.Controls.Add(inspectorButton);
		tools.Controls.Add(toolsButton);

		Controls.Add(tools);
		Controls.Add(navigation);

		commandBindings.Add(new ButtonCommandBinding(sidebarButton, commandRouter, CommandId.ToggleSidebar));
		commandBindings.Add(new ButtonCommandBinding(inspectorButton, commandRouter, CommandId.ToggleInspector));
		commandBindings.Add(new ButtonCommandBinding(toolsButton, commandRouter, CommandId.ToggleToolPanel));
	}

	public event Action<WorkspaceId>? NavigationRequested;

	public void SetActive(WorkspaceId workspace)
	{
		foreach ((WorkspaceId id, ActivityButton button) in navigationButtons)
			button.Selected = id == workspace;
	}

	private void ConfigureActivityButton(
		ActivityButton button,
		WorkspaceDefinition workspace,
		int tabIndex)
	{
		button.Text = workspace.ActivityText;
		button.AccessibleName = workspace.Title;
		button.AccessibleDescription = workspace.Description;
		button.BackColor = Theme.ActivityBar;
		button.Margin = new Padding(0, 0, 0, 4);
		button.TabIndex = tabIndex;
		button.Click += (_, _) => NavigationRequested?.Invoke(workspace.Id);
		toolTip.SetToolTip(button, workspace.Title);
	}

	private void ConfigureToolButton(
		ToolButton button,
		string text,
		string accessibleName,
		int tabIndex)
	{
		button.Text = text;
		button.AccessibleName = accessibleName;
		button.Margin = new Padding(0, 0, 0, 4);
		button.TabIndex = tabIndex;
		toolTip.SetToolTip(button, accessibleName);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			foreach (ButtonCommandBinding binding in commandBindings)
				binding.Dispose();

			toolTip.Dispose();
		}

		base.Dispose(disposing);
	}
}
