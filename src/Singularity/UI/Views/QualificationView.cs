// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Qualification;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.UI.Controls;

namespace Singularity.UI.Views;

public sealed class QualificationView : Panel
{
	private readonly SingularityCheckBox cpuCheck = new();
	private readonly SingularityCheckBox memoryCheck = new();
	private readonly SingularityCheckBox gpuCheck = new();
	private readonly SingularityNumeric cpuThreadsInput = new();
	private readonly SingularityNumeric memoryGbInput = new();
	private readonly SingularityNumeric gpuLoadInput = new();
	private readonly CheckedListBox gpuDeviceInput = new();
	private readonly ComboBox profileInput = new();
	private readonly Label stateValue = CreateValueLabel();
	private readonly Label profileValue = CreateValueLabel();
	private readonly Label modeValue = CreateValueLabel();
	private readonly Label startedValue = CreateValueLabel();
	private readonly Label elapsedValue = CreateValueLabel();
	private readonly Label workloadValue = CreateValueLabel();
	private readonly Label progressLabel = new();
	private readonly MetricBar progressBar = new();
	private readonly Panel feedbackPanel = new();
	private readonly Label feedbackLabel = new();
	private readonly CommandButton resultsButton = new();
	private QualificationProfile selectedProfile = QualificationProfiles.Standard;
	private bool applyingConfiguration;

	public CommandButton StartButton { get; } = new();
	public CommandButton AutoButton { get; } = new();
	public CommandButton StopButton { get; } = new();

	public event Action<QualificationConfiguration>? ConfigurationChanged;
	public event Action? ResultsRequested;

	public QualificationView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Workspace;
		AutoScroll = true;

		BuildUi();
		ApplyConfiguration(QualificationConfiguration.Default);
	}

	public void ApplyConfiguration(QualificationConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		bool wasApplying = applyingConfiguration;
		applyingConfiguration = true;
		try
		{
			selectedProfile = configuration.Profile;
			SelectProfileOption(configuration.Profile.Id);
			cpuCheck.Checked = configuration.EnableCpuWorkload;
			memoryCheck.Checked = configuration.EnableMemoryWorkload;
			gpuCheck.Checked = configuration.EnableGpuWorkload;
			cpuThreadsInput.Value = configuration.CpuThreads;
			memoryGbInput.Value = configuration.MemoryGb;
			gpuLoadInput.Value = configuration.GpuLoadPercent;
			SetGpuSelections(configuration.ResolveSelectedGpuIdentifiers());
			UpdateInputEnabledStates();
		}
		finally
		{
			applyingConfiguration = wasApplying;
		}
	}

	public void UpdateState(QualificationWorkspaceSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		ApplyProfileOptions(
			snapshot.AvailableProfiles,
			snapshot.Configuration.Profile.Id);
		ApplyGpuOptions(
			snapshot.AvailableGpus,
			snapshot.Configuration.ResolveSelectedGpuIdentifiers());
		ApplyConfiguration(snapshot.Configuration);
		SetConfigurationEnabled(
			snapshot.SessionState != QualificationSessionState.Running &&
			snapshot.AutomatedState != QualificationRunState.Running &&
			snapshot.WorkloadState is WorkloadState.Stopped or WorkloadState.Failed);

		stateValue.Text = snapshot.OverallState;
		stateValue.ForeColor = GetStateColor(snapshot);
		profileValue.Text = snapshot.SessionProfile;
		modeValue.Text = snapshot.Mode switch
		{
			QualificationMode.Manual => "MANUAL",
			QualificationMode.Automated => "AUTOMATED",
			_ => "NOT STARTED"
		};
		startedValue.Text = snapshot.StartedAt?.ToString("G") ?? "Not started";
		elapsedValue.Text = snapshot.Elapsed.ToString(@"hh\:mm\:ss");
		workloadValue.Text = string.IsNullOrWhiteSpace(snapshot.WorkloadMessage)
			? snapshot.WorkloadState.ToString()
			: snapshot.WorkloadMessage;

		progressBar.Value = (int)Math.Round(snapshot.ProgressPercent);
		progressLabel.Text = BuildProgressText(snapshot);
		resultsButton.Enabled = snapshot.SessionState is QualificationSessionState.Completed or QualificationSessionState.Failed;

		UpdateFeedback(snapshot.Feedback);
	}

	private void BuildUi()
	{
		Panel header = BuildHeader();
		BuildFeedbackPanel();

		TableLayoutPanel content = new()
		{
			Dock = DockStyle.Fill,
			BackColor = Theme.Workspace,
			Padding = new Padding(ThemeMetrics.SpacingLarge),
			ColumnCount = 2,
			RowCount = 1
		};
		content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
		content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
		content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		Panel configurationPanel = BuildConfigurationPanel();
		Panel sessionPanel = BuildSessionPanel();
		configurationPanel.Margin = new Padding(0, 0, ThemeMetrics.Spacing, 0);
		sessionPanel.Margin = new Padding(0);

		content.Controls.Add(configurationPanel, 0, 0);
		content.Controls.Add(sessionPanel, 1, 0);

		Controls.Add(content);
		Controls.Add(feedbackPanel);
		Controls.Add(header);
	}

	private Panel BuildHeader()
	{
		Panel header = new()
		{
			Dock = DockStyle.Top,
			Height = 86,
			BackColor = Theme.Workspace,
			Padding = new Padding(ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing, ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing)
		};

		FlowLayoutPanel commands = new()
		{
			Dock = DockStyle.Right,
			Width = 430,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			BackColor = Theme.Workspace,
			Padding = new Padding(0, 8, 0, 0)
		};

		ConfigureCommandButton(StartButton, "Start", 120, Theme.Success);
		ConfigureCommandButton(AutoButton, "Automated", 140, Theme.Accent);
		ConfigureCommandButton(StopButton, "Stop / Cancel", 140, Theme.Failure);
		commands.Controls.AddRange([StartButton, AutoButton, StopButton]);

		Label title = new()
		{
			Dock = DockStyle.Top,
			Height = 30,
			Text = "Qualification",
			Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Workspace
		};

		Label subtitle = new()
		{
			Dock = DockStyle.Fill,
			Text = "Configure workloads, review readiness, run qualification, and observe the current session.",
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Workspace,
			TextAlign = ContentAlignment.MiddleLeft
		};

		header.Controls.Add(subtitle);
		header.Controls.Add(title);
		header.Controls.Add(commands);
		return header;
	}

	private void BuildFeedbackPanel()
	{
		feedbackPanel.Dock = DockStyle.Top;
		feedbackPanel.Height = 42;
		feedbackPanel.Padding = new Padding(ThemeMetrics.SpacingLarge, 0, ThemeMetrics.SpacingLarge, 0);
		feedbackPanel.BackColor = Theme.PanelLight;
		feedbackPanel.Visible = false;

		feedbackLabel.Dock = DockStyle.Fill;
		feedbackLabel.Font = ThemeFonts.CardText;
		feedbackLabel.TextAlign = ContentAlignment.MiddleLeft;
		feedbackLabel.BackColor = Theme.PanelLight;
		feedbackLabel.ForeColor = Theme.TextMain;
		feedbackPanel.Controls.Add(feedbackLabel);
	}

	private Panel BuildConfigurationPanel()
	{
		Panel panel = CreateSurface("CONFIGURATION");

		FlowLayoutPanel stack = new()
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoScroll = true,
			BackColor = Theme.Panel,
			Padding = new Padding(ThemeMetrics.Spacing, 0, ThemeMetrics.Spacing, ThemeMetrics.Spacing)
		};

		Panel profilePanel = BuildProfilePanel();
		Panel cpuPanel = BuildWorkloadRow(
			"CPU",
			"Worker threads",
			cpuCheck,
			cpuThreadsInput,
			1,
			Environment.ProcessorCount * 4);
		Panel memoryPanel = BuildWorkloadRow(
			"MEMORY",
			"Allocated GB",
			memoryCheck,
			memoryGbInput,
			1,
			1024);
		Panel gpuPanel = BuildGpuWorkloadRow();

		stack.Controls.AddRange([profilePanel, cpuPanel, memoryPanel, gpuPanel]);
		foreach (Control control in stack.Controls)
			control.Margin = new Padding(0, 0, 0, ThemeMetrics.Spacing);

		void ResizeCards()
		{
			int width = Math.Max(280, stack.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - ThemeMetrics.Spacing * 2);
			foreach (Control control in stack.Controls)
				control.Width = width;
		}

		stack.Resize += (_, _) => ResizeCards();
		panel.Controls.Add(stack);
		ResizeCards();
		return panel;
	}

	private Panel BuildProfilePanel()
	{
		Panel panel = new()
		{
			Height = 112,
			BackColor = Theme.PanelLight,
			Padding = new Padding(ThemeMetrics.Spacing)
		};

		Label title = new()
		{
			Dock = DockStyle.Top,
			Height = 24,
			Text = "Qualification profile",
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight
		};

		Label description = new()
		{
			Dock = DockStyle.Top,
			Height = 24,
			Text = "Built-in and custom profiles define duration and validation thresholds.",
			Font = ThemeFonts.CardTextSmall,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		profileInput.Dock = DockStyle.Bottom;
		profileInput.Height = 34;
		profileInput.DropDownStyle = ComboBoxStyle.DropDownList;
		profileInput.DisplayMember = nameof(QualificationProfile.Name);
		profileInput.Font = ThemeFonts.CardText;
		profileInput.BackColor = Theme.Panel;
		profileInput.ForeColor = Theme.TextMain;
		profileInput.AccessibleName = "Qualification profile";
		profileInput.SelectedIndexChanged += (_, _) =>
		{
			if (applyingConfiguration || profileInput.SelectedItem is not QualificationProfile profile)
				return;

			selectedProfile = profile;
			OnConfigurationEdited();
		};

		panel.Controls.Add(profileInput);
		panel.Controls.Add(description);
		panel.Controls.Add(title);
		return panel;
	}


	private Panel BuildWorkloadRow(
		string name,
		string settingLabel,
		SingularityCheckBox checkBox,
		SingularityNumeric numeric,
		int minimum,
		int maximum)
	{
		Panel panel = new()
		{
			Height = 82,
			BackColor = Theme.PanelLight,
			Padding = new Padding(ThemeMetrics.Spacing)
		};

		TableLayoutPanel layout = CreateWorkloadLayout(1);

		ConfigureWorkloadCheckBox(checkBox);
		Label nameLabel = CreateWorkloadName(name);
		Label setting = CreateWorkloadSetting(settingLabel);
		ConfigureNumeric(numeric, minimum, maximum);

		layout.Controls.Add(checkBox, 0, 0);
		layout.Controls.Add(nameLabel, 1, 0);
		layout.Controls.Add(setting, 2, 0);
		layout.Controls.Add(numeric, 3, 0);
		panel.Controls.Add(layout);
		return panel;
	}

	private Panel BuildGpuWorkloadRow()
	{
		Panel panel = new()
		{
			Height = 148,
			BackColor = Theme.PanelLight,
			Padding = new Padding(ThemeMetrics.Spacing)
		};

		TableLayoutPanel layout = CreateWorkloadLayout(2);
		layout.RowStyles.Clear();
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 66));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

		ConfigureWorkloadCheckBox(gpuCheck);
		Label nameLabel = CreateWorkloadName("GPU");
		Label setting = CreateWorkloadSetting("Target load %");
		ConfigureNumeric(gpuLoadInput, 1, 100);

		gpuDeviceInput.Dock = DockStyle.Fill;
		gpuDeviceInput.Margin = new Padding(0, 2, 0, 4);
		gpuDeviceInput.Font = ThemeFonts.CardText;
		gpuDeviceInput.BackColor = Theme.Panel;
		gpuDeviceInput.ForeColor = Theme.TextMain;
		gpuDeviceInput.BorderStyle = BorderStyle.FixedSingle;
		gpuDeviceInput.CheckOnClick = true;
		gpuDeviceInput.IntegralHeight = false;
		gpuDeviceInput.DisplayMember = nameof(QualificationGpuOption.DisplayName);
		gpuDeviceInput.ValueMember = nameof(QualificationGpuOption.Identifier);
		gpuDeviceInput.AccessibleName = "GPU devices";
		gpuDeviceInput.ItemCheck += (_, _) =>
		{
			if (!applyingConfiguration)
				BeginInvoke(new Action(OnConfigurationEdited));
		};

		layout.Controls.Add(gpuCheck, 0, 0);
		layout.SetRowSpan(gpuCheck, 2);
		layout.Controls.Add(nameLabel, 1, 0);
		layout.SetRowSpan(nameLabel, 2);
		layout.Controls.Add(gpuDeviceInput, 2, 0);
		layout.SetColumnSpan(gpuDeviceInput, 2);
		layout.Controls.Add(setting, 2, 1);
		layout.Controls.Add(gpuLoadInput, 3, 1);
		panel.Controls.Add(layout);
		return panel;
	}

	private static TableLayoutPanel CreateWorkloadLayout(int rowCount)
	{
		TableLayoutPanel layout = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 4,
			RowCount = rowCount,
			BackColor = Theme.PanelLight
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
		if (rowCount == 1)
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		return layout;
	}

	private void ConfigureWorkloadCheckBox(SingularityCheckBox checkBox)
	{
		checkBox.Width = 28;
		checkBox.Height = 28;
		checkBox.Anchor = AnchorStyles.Left;
		checkBox.BackColor = Theme.PanelLight;
		checkBox.CheckedChanged += (_, _) => OnConfigurationEdited();
	}

	private static Label CreateWorkloadName(string name)
	{
		return new Label
		{
			Text = name,
			Dock = DockStyle.Fill,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleLeft
		};
	}

	private static Label CreateWorkloadSetting(string text)
	{
		return new Label
		{
			Text = text,
			Dock = DockStyle.Fill,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleLeft
		};
	}

	private void ConfigureNumeric(SingularityNumeric numeric, int minimum, int maximum)
	{
		numeric.Minimum = minimum;
		numeric.Maximum = maximum;
		numeric.Width = 86;
		numeric.Height = 46;
		numeric.Anchor = AnchorStyles.Right;
		numeric.ValueChanged += (_, _) => OnConfigurationEdited();
	}

	private Panel BuildSessionPanel()
	{
		Panel panel = CreateSurface("CURRENT SESSION");

		TableLayoutPanel rows = new()
		{
			Dock = DockStyle.Top,
			Height = 245,
			ColumnCount = 2,
			RowCount = 6,
			BackColor = Theme.Panel,
			Padding = new Padding(ThemeMetrics.Spacing)
		};
		rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
		rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

		AddSessionRow(rows, 0, "State", stateValue);
		AddSessionRow(rows, 1, "Profile", profileValue);
		AddSessionRow(rows, 2, "Mode", modeValue);
		AddSessionRow(rows, 3, "Started", startedValue);
		AddSessionRow(rows, 4, "Elapsed", elapsedValue);
		AddSessionRow(rows, 5, "Workload", workloadValue);

		Panel progressPanel = new()
		{
			Dock = DockStyle.Top,
			Height = 96,
			BackColor = Theme.Panel,
			Padding = new Padding(ThemeMetrics.Spacing)
		};

		progressLabel.Dock = DockStyle.Top;
		progressLabel.Height = 34;
		progressLabel.Font = ThemeFonts.CardText;
		progressLabel.ForeColor = Theme.TextMain;
		progressLabel.BackColor = Theme.Panel;
		progressLabel.Text = "Manual mode";

		progressBar.Dock = DockStyle.Top;
		progressBar.Height = 10;
		progressBar.FillColor = Theme.Accent;
		progressBar.BackColor = Theme.Panel;

		resultsButton.Dock = DockStyle.Bottom;
		resultsButton.Height = ThemeMetrics.ControlHeight;
		resultsButton.Text = "Open Results";
		resultsButton.AccessibleName = "Open qualification results";
		resultsButton.Click += (_, _) => ResultsRequested?.Invoke();

		progressPanel.Controls.Add(resultsButton);
		progressPanel.Controls.Add(progressBar);
		progressPanel.Controls.Add(progressLabel);

		panel.Controls.Add(progressPanel);
		panel.Controls.Add(rows);
		return panel;
	}

	private void ApplyProfileOptions(
		IReadOnlyList<QualificationProfile> profiles,
		string selectedId)
	{
		bool sameOptions = profileInput.Items.Count == profiles.Count;
		if (sameOptions)
		{
			for (int index = 0; index < profiles.Count; index++)
			{
				if (profileInput.Items[index] is not QualificationProfile existing ||
					!string.Equals(existing.Id, profiles[index].Id, StringComparison.Ordinal) ||
					!string.Equals(existing.Name, profiles[index].Name, StringComparison.Ordinal))
				{
					sameOptions = false;
					break;
				}
			}
		}

		bool wasApplying = applyingConfiguration;
		applyingConfiguration = true;
		try
		{
			if (!sameOptions)
			{
				profileInput.BeginUpdate();
				try
				{
					profileInput.Items.Clear();
					foreach (QualificationProfile profile in profiles)
						profileInput.Items.Add(profile);
				}
				finally { profileInput.EndUpdate(); }
			}
			SelectProfileOption(selectedId);
		}
		finally { applyingConfiguration = wasApplying; }
	}

	private void SelectProfileOption(string id)
	{
		for (int index = 0; index < profileInput.Items.Count; index++)
		{
			if (profileInput.Items[index] is QualificationProfile profile &&
				string.Equals(profile.Id, id, StringComparison.Ordinal))
			{
				if (profileInput.SelectedIndex != index)
					profileInput.SelectedIndex = index;
				selectedProfile = profile;
				return;
			}
		}
	}

	private void ApplyGpuOptions(
		IReadOnlyList<QualificationGpuOption> options,
		IReadOnlyList<string> selectedIdentifiers)
	{
		bool sameOptions = gpuDeviceInput.Items.Count == options.Count;
		if (sameOptions)
		{
			for (int index = 0; index < options.Count; index++)
			{
				if (gpuDeviceInput.Items[index] is not QualificationGpuOption existing ||
					!string.Equals(existing.Identifier, options[index].Identifier, StringComparison.OrdinalIgnoreCase) ||
					!string.Equals(existing.DisplayName, options[index].DisplayName, StringComparison.Ordinal))
				{
					sameOptions = false;
					break;
				}
			}
		}

		bool wasApplying = applyingConfiguration;
		applyingConfiguration = true;
		try
		{
			if (!sameOptions)
			{
				gpuDeviceInput.BeginUpdate();
				try
				{
					gpuDeviceInput.Items.Clear();
					foreach (QualificationGpuOption option in options)
						gpuDeviceInput.Items.Add(option);
				}
				finally
				{
					gpuDeviceInput.EndUpdate();
				}
			}

			SetGpuSelections(selectedIdentifiers);
		}
		finally
		{
			applyingConfiguration = wasApplying;
		}
	}

	private void SetGpuSelections(IReadOnlyList<string> selectedIdentifiers)
	{
		HashSet<string> selected = new(
			selectedIdentifiers,
			StringComparer.OrdinalIgnoreCase);

		for (int index = 0; index < gpuDeviceInput.Items.Count; index++)
		{
			bool shouldBeChecked =
				gpuDeviceInput.Items[index] is QualificationGpuOption option &&
				selected.Contains(option.Identifier);
			if (gpuDeviceInput.GetItemChecked(index) != shouldBeChecked)
				gpuDeviceInput.SetItemChecked(index, shouldBeChecked);
		}
	}

	private void OnConfigurationEdited()
	{
		UpdateInputEnabledStates();
		if (applyingConfiguration)
			return;

		ConfigurationChanged?.Invoke(CreateConfiguration());
	}

	private QualificationConfiguration CreateConfiguration()
	{
		QualificationGpuOption[] selectedGpus = gpuDeviceInput.CheckedItems
			.Cast<QualificationGpuOption>()
			.ToArray();
		string[] selectedIdentifiers = selectedGpus
			.Select(option => option.Identifier)
			.ToArray();

		return new QualificationConfiguration(
			cpuCheck.Checked,
			cpuThreadsInput.Value,
			memoryCheck.Checked,
			memoryGbInput.Value,
			gpuCheck.Checked,
			gpuLoadInput.Value,
			selectedProfile)
		{
			SelectedGpuIdentifiers = Array.AsReadOnly(selectedIdentifiers),
			SelectedGpuIdentifier = selectedIdentifiers.Length == 1
				? selectedIdentifiers[0]
				: null,
			SelectedGpuName = selectedGpus.Length switch
			{
				0 => null,
				1 => selectedGpus[0].DisplayName,
				_ => $"{selectedGpus.Length} GPUs selected"
			}
		};
	}

	private void UpdateInputEnabledStates()
	{
		bool editable = cpuCheck.Enabled;
		cpuThreadsInput.Enabled = editable && cpuCheck.Checked;
		memoryGbInput.Enabled = editable && memoryCheck.Checked;
		gpuLoadInput.Enabled = editable && gpuCheck.Checked;
		gpuDeviceInput.Enabled =
			editable &&
			gpuCheck.Checked &&
			gpuDeviceInput.Items.Count > 0;
	}

	private void SetConfigurationEnabled(bool enabled)
	{
		cpuCheck.Enabled = enabled;
		memoryCheck.Enabled = enabled;
		gpuCheck.Enabled = enabled;
		profileInput.Enabled = enabled;
		UpdateInputEnabledStates();
	}

	private void UpdateFeedback(QualificationFeedback? feedback)
	{
		feedbackPanel.Visible = feedback is not null;
		if (feedback is null)
			return;

		feedbackLabel.Text = feedback.Message;
		feedbackLabel.ForeColor = feedback.Level switch
		{
			QualificationFeedbackLevel.Failure => Theme.Failure,
			QualificationFeedbackLevel.Warning => Theme.Warning,
			_ => Theme.TextMain
		};
	}

	private static Panel CreateSurface(string title)
	{
		Panel panel = new()
		{
			Dock = DockStyle.Fill,
			BackColor = Theme.Panel,
			Padding = new Padding(0, 38, 0, 0)
		};

		Label titleLabel = new()
		{
			Dock = DockStyle.Top,
			Height = 38,
			Top = 0,
			Text = title,
			Font = ThemeFonts.Header,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel,
			Padding = new Padding(ThemeMetrics.Spacing, 0, 0, 0),
			TextAlign = ContentAlignment.MiddleLeft
		};
		panel.Controls.Add(titleLabel);
		titleLabel.BringToFront();
		return panel;
	}

	private static void AddSessionRow(TableLayoutPanel rows, int row, string title, Label value)
	{
		rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 6F));

		Label titleLabel = new()
		{
			Text = title,
			Dock = DockStyle.Fill,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleLeft
		};
		value.Dock = DockStyle.Fill;
		rows.Controls.Add(titleLabel, 0, row);
		rows.Controls.Add(value, 1, row);
	}

	private static Label CreateValueLabel()
	{
		return new Label
		{
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleRight,
			AutoEllipsis = true
		};
	}

	private static void ConfigureCommandButton(
		CommandButton button,
		string text,
		int width,
		Color backColor)
	{
		button.Text = text;
		button.Width = width;
		button.Height = 42;
		button.Margin = new Padding(0, 0, ThemeMetrics.SpacingSmall, 0);
		button.BackColor = backColor;
		button.ForeColor = backColor == Theme.Accent ? Color.Black : Color.White;
		button.AccessibleName = text;
	}

	private static string BuildProgressText(QualificationWorkspaceSnapshot snapshot)
	{
		return snapshot.AutomatedState switch
		{
			QualificationRunState.Running =>
				$"Step {snapshot.StepNumber}/{snapshot.StepCount} • {snapshot.ActiveStep} • {snapshot.ProgressPercent:0}%",
			QualificationRunState.Completed => "Automated qualification complete",
			QualificationRunState.Cancelled => "Automated qualification cancelled",
			QualificationRunState.Failed => "Automated qualification failed",
			_ => "Manual qualification mode"
		};
	}

	private static Color GetStateColor(QualificationWorkspaceSnapshot snapshot)
	{
		if (snapshot.WorkloadState == WorkloadState.Failed ||
			snapshot.SessionState == QualificationSessionState.Failed ||
			snapshot.AutomatedState == QualificationRunState.Failed)
		{
			return Theme.Failure;
		}

		if (snapshot.WorkloadState is WorkloadState.Starting or WorkloadState.Stopping ||
			snapshot.AutomatedState == QualificationRunState.Cancelled)
		{
			return Theme.Warning;
		}

		if (snapshot.WorkloadState == WorkloadState.Running ||
			snapshot.SessionState == QualificationSessionState.Completed ||
			snapshot.AutomatedState == QualificationRunState.Completed)
		{
			return Theme.Success;
		}

		return Theme.TextMain;
	}
}
