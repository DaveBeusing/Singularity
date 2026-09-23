// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Qualification;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.UI.Controls;

namespace Singularity.UI.Views;

public sealed class ResultsView : Panel
{
	private readonly Panel contentPanel = new();
	private readonly Panel emptyPanel = new();
	private readonly Label overallValue = CreateValueLabel();
	private readonly Label profileValue = CreateValueLabel();
	private readonly Label modeValue = CreateValueLabel();
	private readonly Label startedValue = CreateValueLabel();
	private readonly Label finishedValue = CreateValueLabel();
	private readonly Label durationValue = CreateValueLabel();
	private readonly Label cpuValue = CreateValueLabel();
	private readonly Label memoryValue = CreateValueLabel();
	private readonly Label gpuValue = CreateValueLabel();
	private readonly Label cpuMetricValue = CreateValueLabel();
	private readonly Label memoryMetricValue = CreateValueLabel();
	private readonly Label gpuMetricValue = CreateValueLabel();
	private readonly Label temperatureMetricValue = CreateValueLabel();
	private readonly Label powerMetricValue = CreateValueLabel();
	private readonly Label vramMetricValue = CreateValueLabel();
	private readonly FlowLayoutPanel gpuEvidenceList = new();
	private readonly TelemetryTimelineView timelineView = new();

	public ResultsView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Workspace;
		AutoScroll = true;
		BuildUi();
		UpdateState(ResultsWorkspaceSnapshot.Empty);
	}

	public void UpdateState(ResultsWorkspaceSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		emptyPanel.Visible = !snapshot.HasResult;
		contentPanel.Visible = snapshot.HasResult;
		if (!snapshot.HasResult)
			return;

		SetStatus(overallValue, snapshot.OverallStatus);
		profileValue.Text = snapshot.ProfileName;
		modeValue.Text = FormatMode(snapshot.ExecutionMode);
		startedValue.Text = snapshot.StartedAt?.ToString("G") ?? "Unavailable";
		finishedValue.Text = snapshot.FinishedAt?.ToString("G") ?? "Unavailable";
		durationValue.Text = snapshot.Duration.ToString(@"hh\:mm\:ss");

		SetStatus(cpuValue, snapshot.CpuStatus);
		SetStatus(memoryValue, snapshot.MemoryStatus);
		SetStatus(gpuValue, snapshot.GpuStatus);

		SessionTelemetryStatistics statistics = snapshot.TelemetryStatistics;
		cpuMetricValue.Text = FormatMetric(statistics.CpuLoadPercent, "%");
		memoryMetricValue.Text = FormatMetric(statistics.SystemMemoryUsagePercent, "%");
		gpuMetricValue.Text = FormatMetric(statistics.GpuLoadPercent, "%");
		temperatureMetricValue.Text = FormatMetric(statistics.GpuTemperatureCelsius, "°C");
		powerMetricValue.Text = FormatMetric(statistics.GpuPowerWatts, "W");
		vramMetricValue.Text = FormatMetric(statistics.GpuVramUsagePercent, "%");
		timelineView.UpdateTimeline(snapshot.TelemetryTimeline);
		RenderGpuEvidence(snapshot.GpuEvidence);
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
			Text = "Qualification Results",
			Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Workspace,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label subtitle = new()
		{
			Dock = DockStyle.Fill,
			Text = "Latest completed qualification evidence and telemetry statistics.",
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Workspace,
			TextAlign = ContentAlignment.MiddleLeft
		};

		header.Controls.Add(subtitle);
		header.Controls.Add(title);

		emptyPanel.Dock = DockStyle.Fill;
		emptyPanel.BackColor = Theme.Workspace;
		Label emptyLabel = new()
		{
			Dock = DockStyle.Fill,
			Text = "No completed qualification result is available yet.\r\nRun a qualification to populate this workspace.",
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Workspace,
			TextAlign = ContentAlignment.MiddleCenter
		};
		emptyPanel.Controls.Add(emptyLabel);

		contentPanel.Dock = DockStyle.Top;
		contentPanel.AutoSize = true;
		contentPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		contentPanel.BackColor = Theme.Workspace;
		contentPanel.Padding = new Padding(ThemeMetrics.SpacingLarge);

		TableLayoutPanel layout = new()
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			ColumnCount = 2,
			RowCount = 4,
			BackColor = Theme.Workspace
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		Control summary = CreateSummaryCard();
		Control validation = CreateValidationCard();
		Control statistics = CreateStatisticsCard();
		Control gpuEvidence = CreateGpuEvidenceCard();
		summary.Margin = new Padding(0, 0, ThemeMetrics.Spacing, ThemeMetrics.Spacing);
		validation.Margin = new Padding(0, 0, 0, ThemeMetrics.Spacing);
		statistics.Margin = new Padding(0, 0, 0, ThemeMetrics.Spacing);
		timelineView.Margin = new Padding(0, 0, 0, ThemeMetrics.Spacing);
		gpuEvidence.Margin = new Padding(0);
		layout.Controls.Add(summary, 0, 0);
		layout.Controls.Add(validation, 1, 0);
		layout.Controls.Add(statistics, 0, 1);
		layout.SetColumnSpan(statistics, 2);
		layout.Controls.Add(timelineView, 0, 2);
		layout.SetColumnSpan(timelineView, 2);
		layout.Controls.Add(gpuEvidence, 0, 3);
		layout.SetColumnSpan(gpuEvidence, 2);

		contentPanel.Controls.Add(layout);
		Controls.Add(emptyPanel);
		Controls.Add(contentPanel);
		Controls.Add(header);
	}

	private Control CreateSummaryCard()
	{
		TableLayoutPanel grid = CreateCard("RESULT SUMMARY", 6);
		AddRow(grid, 0, "Overall", overallValue);
		AddRow(grid, 1, "Profile", profileValue);
		AddRow(grid, 2, "Mode", modeValue);
		AddRow(grid, 3, "Started", startedValue);
		AddRow(grid, 4, "Finished", finishedValue);
		AddRow(grid, 5, "Duration", durationValue);
		return grid;
	}

	private Control CreateValidationCard()
	{
		TableLayoutPanel grid = CreateCard("VALIDATION", 4);
		AddRow(grid, 0, "Overall", overallValue: CreateStatusMirror(overallValue));
		AddRow(grid, 1, "CPU", cpuValue);
		AddRow(grid, 2, "Memory", memoryValue);
		AddRow(grid, 3, "GPU", gpuValue);
		return grid;
	}

	private Control CreateStatisticsCard()
	{
		TableLayoutPanel grid = CreateCard("TELEMETRY STATISTICS — MIN / AVG / MAX", 6);
		AddRow(grid, 0, "CPU load", cpuMetricValue);
		AddRow(grid, 1, "System memory", memoryMetricValue);
		AddRow(grid, 2, "GPU load", gpuMetricValue);
		AddRow(grid, 3, "GPU temperature", temperatureMetricValue);
		AddRow(grid, 4, "GPU power", powerMetricValue);
		AddRow(grid, 5, "VRAM usage", vramMetricValue);
		return grid;
	}

	private Control CreateGpuEvidenceCard()
	{
		Panel panel = new()
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			BackColor = Theme.Panel,
			Padding = new Padding(ThemeMetrics.Spacing)
		};

		Label title = new()
		{
			Dock = DockStyle.Top,
			Height = 32,
			Text = "PER-DEVICE GPU EVIDENCE",
			Font = ThemeFonts.Header,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel
		};

		gpuEvidenceList.Dock = DockStyle.Top;
		gpuEvidenceList.AutoSize = true;
		gpuEvidenceList.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		gpuEvidenceList.FlowDirection = FlowDirection.TopDown;
		gpuEvidenceList.WrapContents = false;
		gpuEvidenceList.BackColor = Theme.Panel;

		panel.Controls.Add(gpuEvidenceList);
		panel.Controls.Add(title);
		return panel;
	}

	private void RenderGpuEvidence(IReadOnlyList<GpuQualificationEvidence> evidence)
	{
		gpuEvidenceList.SuspendLayout();
		try
		{
			gpuEvidenceList.Controls.Clear();
			if (evidence.Count == 0)
			{
				gpuEvidenceList.Controls.Add(new Label
				{
					AutoSize = true,
					Text = "No explicit GPU device evidence was recorded.",
					Font = ThemeFonts.CardText,
					ForeColor = Theme.TextMuted,
					BackColor = Theme.Panel
				});
				return;
			}

			foreach (GpuQualificationEvidence gpu in evidence)
			{
				Panel card = new()
				{
					Width = Math.Max(360, contentPanel.ClientSize.Width - ThemeMetrics.SpacingLarge * 2),
					Height = 92,
					BackColor = Theme.PanelLight,
					Margin = new Padding(0, 0, 0, ThemeMetrics.SpacingSmall),
					Padding = new Padding(ThemeMetrics.Spacing)
				};

				Label heading = new()
				{
					Dock = DockStyle.Top,
					Height = 24,
					Text = $"{gpu.Name}  •  {StatusStyle.Format(gpu.Result)}",
					Font = ThemeFonts.CardTitle,
					ForeColor = StatusStyle.GetColor(gpu.Result),
					BackColor = Theme.PanelLight,
					AutoEllipsis = true
				};
				Label details = new()
				{
					Dock = DockStyle.Fill,
					Text =
						$"{gpu.Identifier}\r\n" +
						$"Load {FormatMetric(gpu.TelemetryStatistics.LoadPercent, "%")}  •  " +
						$"Temp {FormatMetric(gpu.TelemetryStatistics.TemperatureCelsius, "°C")}  •  " +
						$"Power {FormatMetric(gpu.TelemetryStatistics.PowerWatts, "W")}  •  " +
						$"VRAM {FormatMetric(gpu.TelemetryStatistics.VramUsagePercent, "%")}  •  " +
						$"Unavailable samples {gpu.TelemetryStatistics.UnavailableSampleCount}",
					Font = ThemeFonts.CardTextSmall,
					ForeColor = Theme.TextMuted,
					BackColor = Theme.PanelLight,
					AutoEllipsis = true
				};

				card.Controls.Add(details);
				card.Controls.Add(heading);
				gpuEvidenceList.Controls.Add(card);
			}
		}
		finally
		{
			gpuEvidenceList.ResumeLayout();
		}
	}

	private static TableLayoutPanel CreateCard(string title, int valueRows)
	{
		TableLayoutPanel grid = new()
		{
			Dock = DockStyle.Fill,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			BackColor = Theme.Panel,
			Padding = new Padding(ThemeMetrics.Spacing),
			ColumnCount = 2,
			RowCount = valueRows + 1,
			MinimumSize = new Size(220, 0)
		};
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

		Label titleLabel = new()
		{
			Dock = DockStyle.Fill,
			AutoSize = true,
			Text = title,
			Font = ThemeFonts.Header,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel,
			Padding = new Padding(0, 0, 0, ThemeMetrics.SpacingSmall)
		};
		grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		grid.Controls.Add(titleLabel, 0, 0);
		grid.SetColumnSpan(titleLabel, 2);

		for (int row = 1; row <= valueRows; row++)
			grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

		return grid;
	}

	private static void AddRow(TableLayoutPanel grid, int row, string label, Label overallValue)
	{
		int targetRow = row + 1;
		Label name = new()
		{
			Dock = DockStyle.Fill,
			Text = label,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleLeft
		};
		overallValue.Dock = DockStyle.Fill;
		grid.Controls.Add(name, 0, targetRow);
		grid.Controls.Add(overallValue, 1, targetRow);
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

	private static Label CreateStatusMirror(Label source)
	{
		Label mirror = CreateValueLabel();
		source.TextChanged += (_, _) => mirror.Text = source.Text;
		source.ForeColorChanged += (_, _) => mirror.ForeColor = source.ForeColor;
		mirror.Text = source.Text;
		mirror.ForeColor = source.ForeColor;
		return mirror;
	}

	private static void SetStatus(Label label, ValidationStatus status)
	{
		label.Text = StatusStyle.Format(status);
		label.ForeColor = StatusStyle.GetColor(status);
	}

	private static string FormatMode(QualificationExecutionMode mode) =>
		mode == QualificationExecutionMode.Unknown
			? "Unavailable"
			: mode.ToString().ToUpperInvariant();

	private static string FormatMetric(MetricStatistics? statistics, string unit)
	{
		return statistics is null
			? "Unavailable"
			: $"{statistics.Minimum:0.0} / {statistics.Average:0.0} / {statistics.Maximum:0.0} {unit}";
	}
}
