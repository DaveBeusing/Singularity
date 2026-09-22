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
	private readonly Dictionary<string, CommandButton> profileButtons = [];
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

		applyingConfiguration = true;
		try
		{
			selectedProfile = configuration.Profile;
			cpuCheck.Checked = configuration.EnableCpuWorkload;
			memoryCheck.Checked = configuration.EnableMemoryWorkload;
			gpuCheck.Checked = configuration.EnableGpuWorkload;
			cpuThreadsInput.Value = configuration.CpuThreads;
			memoryGbInput.Value = configuration.MemoryGb;
			gpuLoadInput.Value = configuration.GpuLoadPercent;
			UpdateInputEnabledStates();
			UpdateProfileButtonStyles();
		}
		finally
		{
			applyingConfiguration = false;
		}
	}

	public void UpdateState(QualificationWorkspaceSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		ApplyConfiguration(snapshot.Configuration);

		stateValue.Text = snapshot.OverallState;
		stateValue.ForeColor = GetStateColor(snapshot);
		profileValue.Text = snapshot.Configuration.Profile.Name;
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
		Panel gpuPanel = BuildWorkloadRow(
			"GPU",
			"Target load %",
			gpuCheck,
			gpuLoadInput,
			1,
			100);

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
			Text = "Profiles define duration and validation thresholds.",
			Font = ThemeFonts.CardTextSmall,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		FlowLayoutPanel buttons = new()
		{
			Dock = DockStyle.Bottom,
			Height = 42,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			BackColor = Theme.PanelLight
		};

		foreach (QualificationProfile profile in QualificationProfiles.All)
		{
			CommandButton button = new()
			{
				Text = profile.Name,
				Width = 112,
				Height = 34,
				Margin = new Padding(0, 4, ThemeMetrics.SpacingSmall, 0),
				Tag = profile,
				AccessibleName = $"{profile.Name} qualification profile"
			};
			button.Click += (_, _) => SelectProfile(profile);
			profileButtons.Add(profile.Name, button);
			buttons.Controls.Add(button);
		}

		panel.Controls.Add(buttons);
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

		TableLayoutPanel layout = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 4,
			RowCount = 1,
			BackColor = Theme.PanelLight
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		checkBox.Width = 28;
		checkBox.Height = 28;
		checkBox.Anchor = AnchorStyles.Left;
		checkBox.BackColor = Theme.PanelLight;
		checkBox.CheckedChanged += (_, _) => OnConfigurationEdited();

		Label nameLabel = new()
		{
			Text = name,
			Dock = DockStyle.Fill,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label setting = new()
		{
			Text = settingLabel,
			Dock = DockStyle.Fill,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleLeft
		};

		numeric.Minimum = minimum;
		numeric.Maximum = maximum;
		numeric.Width = 86;
		numeric.Height = 46;
		numeric.Anchor = AnchorStyles.Right;
		numeric.ValueChanged += (_, _) => OnConfigurationEdited();

		layout.Controls.Add(checkBox, 0, 0);
		layout.Controls.Add(nameLabel, 1, 0);
		layout.Controls.Add(setting, 2, 0);
		layout.Controls.Add(numeric, 3, 0);
		panel.Controls.Add(layout);
		return panel;
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

	private void SelectProfile(QualificationProfile profile)
	{
		selectedProfile = profile;
		UpdateProfileButtonStyles();
		OnConfigurationEdited();
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
		return new QualificationConfiguration(
			cpuCheck.Checked,
			cpuThreadsInput.Value,
			memoryCheck.Checked,
			memoryGbInput.Value,
			gpuCheck.Checked,
			gpuLoadInput.Value,
			selectedProfile);
	}

	private void UpdateInputEnabledStates()
	{
		cpuThreadsInput.Enabled = cpuCheck.Checked;
		memoryGbInput.Enabled = memoryCheck.Checked;
		gpuLoadInput.Enabled = gpuCheck.Checked;
	}

	private void UpdateProfileButtonStyles()
	{
		foreach ((string name, CommandButton button) in profileButtons)
		{
			bool selected = string.Equals(name, selectedProfile.Name, StringComparison.Ordinal);
			button.BackColor = selected ? Theme.Accent : Theme.Panel;
			button.ForeColor = selected ? Color.Black : Theme.TextMain;
			button.FlatAppearance.BorderSize = selected ? 1 : 0;
			button.FlatAppearance.BorderColor = Theme.Accent;
		}
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
