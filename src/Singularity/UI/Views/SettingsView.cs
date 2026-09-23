// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application.Persistence;
using Singularity.Core.Validation;
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
	private readonly ListBox profileList = new();
	private readonly CommandButton createProfileButton = new();
	private readonly CommandButton duplicateProfileButton = new();
	private readonly CommandButton editProfileButton = new();
	private readonly CommandButton deleteProfileButton = new();
	private readonly CommandButton resetProfilesButton = new();
	private readonly Label profileStatusLabel = new();
	private bool applyingState;

	public event Action<bool>? SidebarVisibilityChanged;
	public event Action<bool>? InspectorVisibilityChanged;
	public event Action<bool>? ToolPanelVisibilityChanged;
	public event Action? ResetLayoutRequested;
	public event Action? ClearQualificationArchiveRequested;
	public event Action? CreateQualificationProfileRequested;
	public event Action<QualificationProfile>? DuplicateQualificationProfileRequested;
	public event Action<QualificationProfile>? EditQualificationProfileRequested;
	public event Action<QualificationProfile>? DeleteQualificationProfileRequested;
	public event Action? ResetQualificationProfilesRequested;

	public SettingsView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Workspace;
		AutoScroll = true;
		BuildUi();

		sidebarCheck.CheckedChanged += (_, _) => { if (!applyingState) SidebarVisibilityChanged?.Invoke(sidebarCheck.Checked); };
		inspectorCheck.CheckedChanged += (_, _) => { if (!applyingState) InspectorVisibilityChanged?.Invoke(inspectorCheck.Checked); };
		toolPanelCheck.CheckedChanged += (_, _) => { if (!applyingState) ToolPanelVisibilityChanged?.Invoke(toolPanelCheck.Checked); };
		resetButton.Click += (_, _) => ResetLayoutRequested?.Invoke();
		clearArchiveButton.Click += (_, _) => ClearQualificationArchiveRequested?.Invoke();
		createProfileButton.Click += (_, _) => CreateQualificationProfileRequested?.Invoke();
		duplicateProfileButton.Click += (_, _) => { if (SelectedProfile is { } profile) DuplicateQualificationProfileRequested?.Invoke(profile); };
		editProfileButton.Click += (_, _) => { if (SelectedProfile is { } profile) EditQualificationProfileRequested?.Invoke(profile); };
		deleteProfileButton.Click += (_, _) => { if (SelectedProfile is { } profile) DeleteQualificationProfileRequested?.Invoke(profile); };
		resetProfilesButton.Click += (_, _) => ResetQualificationProfilesRequested?.Invoke();
		profileList.SelectedIndexChanged += (_, _) => UpdateProfileButtons();
	}

	private QualificationProfile? SelectedProfile =>
		(profileList.SelectedItem as ProfileListItem)?.Profile;

	public void UpdateState(ShellLayoutState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		applyingState = true;
		try
		{
			sidebarCheck.Checked = state.SidebarVisible;
			inspectorCheck.Checked = state.InspectorVisible;
			toolPanelCheck.Checked = state.ToolPanelVisible;
		}
		finally { applyingState = false; }
	}

	public void UpdateArchiveState(QualificationArchiveState state, int recordCount, string? error, string? archivePath)
	{
		archiveStatusLabel.Text = state switch
		{
			QualificationArchiveState.Loading => "Loading local qualification evidence...",
			QualificationArchiveState.Ready when recordCount == 0 => $"Ready • No stored evidence • {archivePath ?? "Local application data"}",
			QualificationArchiveState.Ready => $"Ready • {recordCount} stored record(s) • {archivePath ?? "Local application data"}",
			QualificationArchiveState.Failed => $"Unavailable • {error ?? "Archive operation failed."}",
			_ => "Not loaded"
		};
		archiveStatusLabel.ForeColor = state == QualificationArchiveState.Failed ? Theme.Failure : Theme.TextMuted;
		clearArchiveButton.Enabled = state == QualificationArchiveState.Ready && recordCount > 0;
	}

	public void UpdateProfileState(
		IReadOnlyList<QualificationProfile> profiles,
		QualificationProfileStoreState state,
		string? error,
		string storagePath)
	{
		string? selectedId = SelectedProfile?.Id;
		profileList.BeginUpdate();
		try
		{
			profileList.Items.Clear();
			foreach (QualificationProfile profile in profiles)
				profileList.Items.Add(new ProfileListItem(profile.Snapshot()));
		}
		finally { profileList.EndUpdate(); }

		int selectedIndex = 0;
		if (!string.IsNullOrWhiteSpace(selectedId))
		{
			for (int index = 0; index < profileList.Items.Count; index++)
			{
				if (profileList.Items[index] is ProfileListItem item &&
					string.Equals(item.Profile.Id, selectedId, StringComparison.Ordinal))
				{
					selectedIndex = index;
					break;
				}
			}
		}
		if (profileList.Items.Count > 0)
			profileList.SelectedIndex = selectedIndex;

		int customCount = profiles.Count(profile => !profile.IsBuiltIn);
		profileStatusLabel.Text = state switch
		{
			QualificationProfileStoreState.Loading => "Loading custom profiles...",
			QualificationProfileStoreState.Ready => $"Ready • {customCount} custom profile(s) • {storagePath}",
			QualificationProfileStoreState.Failed => $"Unavailable • {error ?? "Profile storage failed."}",
			_ => $"Not loaded • {storagePath}"
		};
		profileStatusLabel.ForeColor = state == QualificationProfileStoreState.Failed ? Theme.Failure : Theme.TextMuted;
		resetProfilesButton.Enabled = customCount > 0 && state == QualificationProfileStoreState.Ready;
		UpdateProfileButtons();
	}

	private void BuildUi()
	{
		Panel header = new()
		{
			Dock = DockStyle.Top, Height = 78, BackColor = Theme.Workspace,
			Padding = new Padding(ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing, ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing)
		};
		header.Controls.Add(new Label
		{
			Dock = DockStyle.Fill, Text = "Manage shell layout, local qualification evidence, and reusable qualification profiles.",
			Font = ThemeFonts.Subtitle, ForeColor = Theme.TextMuted, BackColor = Theme.Workspace
		});
		header.Controls.Add(new Label
		{
			Dock = DockStyle.Top, Height = 28, Text = "Settings", Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain, BackColor = Theme.Workspace
		});

		FlowLayoutPanel stack = new()
		{
			Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown,
			WrapContents = false, BackColor = Theme.Workspace,
			Padding = new Padding(ThemeMetrics.SpacingLarge)
		};
		stack.Controls.Add(BuildLayoutSurface());
		stack.Controls.Add(BuildProfileSurface());
		Controls.Add(stack);
		Controls.Add(header);
	}

	private Panel BuildLayoutSurface()
	{
		Panel surface = new() { Width = 980, Height = 410, BackColor = Theme.Panel, Padding = new Padding(ThemeMetrics.SpacingLarge) };
		TableLayoutPanel grid = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 6, BackColor = Theme.Panel };
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
		for (int row = 0; row < 6; row++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
		AddPreferenceRow(grid, 0, sidebarCheck, "Context sidebar", "Show the contextual navigation sidebar.");
		AddPreferenceRow(grid, 1, inspectorCheck, "Inspector", "Show the inspector on workspaces that support it.");
		AddPreferenceRow(grid, 2, toolPanelCheck, "Tool panel", "Show the bottom tool panel on workspaces that support it.");
		ConfigureButton(resetButton, "Reset layout to defaults", 190);
		grid.Controls.Add(resetButton, 1, 3); grid.SetColumnSpan(resetButton, 2);
		grid.Controls.Add(CreateTitle("Qualification archive"), 1, 4);
		ConfigureStatusLabel(archiveStatusLabel, "Not loaded");
		grid.Controls.Add(archiveStatusLabel, 2, 4);
		ConfigureButton(clearArchiveButton, "Clear qualification archive", 210);
		clearArchiveButton.Enabled = false;
		grid.Controls.Add(clearArchiveButton, 1, 5); grid.SetColumnSpan(clearArchiveButton, 2);
		surface.Controls.Add(grid);
		surface.Controls.Add(CreateSectionTitle("LAYOUT & LOCAL DATA"));
		return surface;
	}

	private Panel BuildProfileSurface()
	{
		Panel surface = new() { Width = 980, Height = 330, BackColor = Theme.Panel, Padding = new Padding(ThemeMetrics.SpacingLarge), Margin = new Padding(0, ThemeMetrics.Spacing, 0, 0) };
		TableLayoutPanel grid = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, BackColor = Theme.Panel };
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
		grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
		grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
		grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

		ConfigureStatusLabel(profileStatusLabel, "Not loaded");
		grid.Controls.Add(profileStatusLabel, 0, 0); grid.SetColumnSpan(profileStatusLabel, 2);
		profileList.Dock = DockStyle.Fill; profileList.Font = ThemeFonts.CardText; profileList.BackColor = Theme.PanelLight; profileList.ForeColor = Theme.TextMain; profileList.BorderStyle = BorderStyle.FixedSingle;
		grid.Controls.Add(profileList, 0, 1); grid.SetColumnSpan(profileList, 2);

		FlowLayoutPanel actions = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Theme.Panel };
		ConfigureButton(createProfileButton, "Create", 100);
		ConfigureButton(duplicateProfileButton, "Duplicate", 110);
		ConfigureButton(editProfileButton, "Edit", 90);
		ConfigureButton(deleteProfileButton, "Delete", 90);
		actions.Controls.AddRange([createProfileButton, duplicateProfileButton, editProfileButton, deleteProfileButton]);
		grid.Controls.Add(actions, 0, 2); grid.SetColumnSpan(actions, 2);
		ConfigureButton(resetProfilesButton, "Delete all custom profiles", 210);
		grid.Controls.Add(resetProfilesButton, 0, 3); grid.SetColumnSpan(resetProfilesButton, 2);

		surface.Controls.Add(grid);
		surface.Controls.Add(CreateSectionTitle("QUALIFICATION PROFILES"));
		return surface;
	}

	private void UpdateProfileButtons()
	{
		QualificationProfile? selected = SelectedProfile;
		duplicateProfileButton.Enabled = selected is not null;
		editProfileButton.Enabled = selected is { IsBuiltIn: false };
		deleteProfileButton.Enabled = selected is { IsBuiltIn: false };
	}

	private static void ConfigureButton(CommandButton button, string text, int width)
	{
		button.Text = text; button.Width = width; button.Height = ThemeMetrics.ControlHeight;
		button.Anchor = AnchorStyles.Left; button.AccessibleName = text;
	}

	private static Label CreateSectionTitle(string text) => new()
	{
		Dock = DockStyle.Top, Height = 34, Text = text, Font = ThemeFonts.Header,
		ForeColor = Theme.TextMain, BackColor = Theme.Panel
	};

	private static Label CreateTitle(string text) => new()
	{
		Dock = DockStyle.Fill, Text = text, Font = ThemeFonts.CardTitle,
		ForeColor = Theme.TextMain, BackColor = Theme.Panel, TextAlign = ContentAlignment.MiddleLeft
	};

	private static void ConfigureStatusLabel(Label label, string text)
	{
		label.Dock = DockStyle.Fill; label.Font = ThemeFonts.CardText; label.ForeColor = Theme.TextMuted;
		label.BackColor = Theme.Panel; label.TextAlign = ContentAlignment.MiddleLeft; label.AutoEllipsis = true; label.Text = text;
	}

	private static void AddPreferenceRow(TableLayoutPanel grid, int row, SingularityCheckBox checkBox, string title, string description)
	{
		checkBox.Anchor = AnchorStyles.Left; checkBox.AccessibleName = title;
		grid.Controls.Add(checkBox, 0, row);
		grid.Controls.Add(CreateTitle(title), 1, row);
		grid.Controls.Add(new Label
		{
			Dock = DockStyle.Fill, Text = description, Font = ThemeFonts.CardText, ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
		}, 2, row);
	}

	private sealed record ProfileListItem(QualificationProfile Profile)
	{
		public override string ToString() =>
			Profile.IsBuiltIn ? $"{Profile.Name}  •  Built-in" : $"{Profile.Name}  •  Custom";
	}
}
