// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application.Persistence;
using Singularity.UI.Controls;
using Singularity.UI.Shell;

namespace Singularity.UI.Views;

public sealed class SettingsView : Panel
{
	private readonly SingularityCheckBox sidebarCheck = new();
	private readonly SingularityCheckBox inspectorCheck = new();
	private readonly SingularityCheckBox toolPanelCheck = new();
	private readonly CommandButton resetButton = new();
	private readonly CommandButton clearArchiveButton = new();
	private readonly Label archiveStatusLabel = new();
	private bool applyingState;

	public event Action<bool>? SidebarVisibilityChanged;
	public event Action<bool>? InspectorVisibilityChanged;
	public event Action<bool>? ToolPanelVisibilityChanged;
	public event Action? ResetLayoutRequested;
	public event Action? ClearQualificationArchiveRequested;

	public SettingsView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Workspace;
		AutoScroll = true;
		BuildUi();

		sidebarCheck.CheckedChanged += (_, _) =>
		{
			if (!applyingState)
				SidebarVisibilityChanged?.Invoke(sidebarCheck.Checked);
		};
		inspectorCheck.CheckedChanged += (_, _) =>
		{
			if (!applyingState)
				InspectorVisibilityChanged?.Invoke(inspectorCheck.Checked);
		};
		toolPanelCheck.CheckedChanged += (_, _) =>
		{
			if (!applyingState)
				ToolPanelVisibilityChanged?.Invoke(toolPanelCheck.Checked);
		};
		resetButton.Click += (_, _) => ResetLayoutRequested?.Invoke();
		clearArchiveButton.Click += (_, _) => ClearQualificationArchiveRequested?.Invoke();
	}

	public void UpdateState(ShellLayoutState state)
	{
		ArgumentNullException.ThrowIfNull(state);

		applyingState = true;
		try
		{
			if (sidebarCheck.Checked != state.SidebarVisible)
				sidebarCheck.Checked = state.SidebarVisible;
			if (inspectorCheck.Checked != state.InspectorVisible)
				inspectorCheck.Checked = state.InspectorVisible;
			if (toolPanelCheck.Checked != state.ToolPanelVisible)
				toolPanelCheck.Checked = state.ToolPanelVisible;
		}
		finally
		{
			applyingState = false;
		}
	}

	public void UpdateArchiveState(
		QualificationArchiveState state,
		int recordCount,
		string? error,
		string? archivePath)
	{
		archiveStatusLabel.Text = state switch
		{
			QualificationArchiveState.Loading => "Loading local qualification evidence...",
			QualificationArchiveState.Ready when recordCount == 0 =>
				$"Ready • No stored evidence • {archivePath ?? "Local application data"}",
			QualificationArchiveState.Ready =>
				$"Ready • {recordCount} stored record(s) • {archivePath ?? "Local application data"}",
			QualificationArchiveState.Failed =>
				$"Unavailable • {error ?? "Archive operation failed."}",
			_ => "Not loaded"
		};

		archiveStatusLabel.ForeColor = state == QualificationArchiveState.Failed
			? Theme.Error
			: Theme.TextMuted;
		clearArchiveButton.Enabled =
			state == QualificationArchiveState.Ready && recordCount > 0;
	}

	private void BuildUi()
	{
		Panel header = new()
		{
			Dock = DockStyle.Top,
			Height = 78,
			BackColor = Theme.Workspace,
			Padding = new Padding(ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing, ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing)
		};
		Label title = new()
		{
			Dock = DockStyle.Top,
			Height = 28,
			Text = "Settings",
			Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Workspace
		};
		Label subtitle = new()
		{
			Dock = DockStyle.Fill,
			Text = "Shell layout is session-only. Completed qualification evidence is retained locally for this Windows user.",
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Workspace
		};
		header.Controls.Add(subtitle);
		header.Controls.Add(title);

		Panel surface = new()
		{
			Dock = DockStyle.Top,
			Height = 410,
			BackColor = Theme.Panel,
			Padding = new Padding(ThemeMetrics.SpacingLarge)
		};

		TableLayoutPanel grid = new()
		{
			Dock = DockStyle.Top,
			Height = 310,
			ColumnCount = 3,
			RowCount = 6,
			BackColor = Theme.Panel
		};
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
		for (int row = 0; row < 6; row++)
			grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

		AddPreferenceRow(grid, 0, sidebarCheck, "Context sidebar", "Show the contextual navigation sidebar.");
		AddPreferenceRow(grid, 1, inspectorCheck, "Inspector", "Show the inspector on workspaces that support it.");
		AddPreferenceRow(grid, 2, toolPanelCheck, "Tool panel", "Show the bottom tool panel on workspaces that support it.");

		resetButton.Text = "Reset layout to defaults";
		resetButton.Width = 190;
		resetButton.Height = ThemeMetrics.ControlHeight;
		resetButton.Anchor = AnchorStyles.Left;
		resetButton.AccessibleName = "Reset shell layout to defaults";
		grid.Controls.Add(resetButton, 1, 3);
		grid.SetColumnSpan(resetButton, 2);

		Label archiveTitle = new()
		{
			Dock = DockStyle.Fill,
			Text = "Qualification archive",
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleLeft
		};
		archiveStatusLabel.Dock = DockStyle.Fill;
		archiveStatusLabel.Font = ThemeFonts.CardText;
		archiveStatusLabel.ForeColor = Theme.TextMuted;
		archiveStatusLabel.BackColor = Theme.Panel;
		archiveStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
		archiveStatusLabel.AutoEllipsis = true;
		archiveStatusLabel.Text = "Not loaded";
		grid.Controls.Add(archiveTitle, 1, 4);
		grid.Controls.Add(archiveStatusLabel, 2, 4);

		clearArchiveButton.Text = "Clear qualification archive";
		clearArchiveButton.Width = 210;
		clearArchiveButton.Height = ThemeMetrics.ControlHeight;
		clearArchiveButton.Anchor = AnchorStyles.Left;
		clearArchiveButton.AccessibleName = "Clear locally stored qualification evidence";
		clearArchiveButton.Enabled = false;
		grid.Controls.Add(clearArchiveButton, 1, 5);
		grid.SetColumnSpan(clearArchiveButton, 2);

		Label sectionTitle = new()
		{
			Dock = DockStyle.Top,
			Height = 34,
			Text = "LAYOUT & LOCAL DATA",
			Font = ThemeFonts.Header,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel
		};

		surface.Controls.Add(grid);
		surface.Controls.Add(sectionTitle);

		Panel content = new()
		{
			Dock = DockStyle.Fill,
			BackColor = Theme.Workspace,
			Padding = new Padding(ThemeMetrics.SpacingLarge)
		};
		content.Controls.Add(surface);

		Controls.Add(content);
		Controls.Add(header);
	}

	private static void AddPreferenceRow(
		TableLayoutPanel grid,
		int row,
		SingularityCheckBox checkBox,
		string title,
		string description)
	{
		checkBox.Anchor = AnchorStyles.Left;
		checkBox.AccessibleName = title;

		Label titleLabel = new()
		{
			Dock = DockStyle.Fill,
			Text = title,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label descriptionLabel = new()
		{
			Dock = DockStyle.Fill,
			Text = description,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleLeft,
			AutoEllipsis = true
		};

		grid.Controls.Add(checkBox, 0, row);
		grid.Controls.Add(titleLabel, 1, row);
		grid.Controls.Add(descriptionLabel, 2, row);
	}
}
