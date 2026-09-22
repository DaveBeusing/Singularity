// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Qualification;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.UI.Controls;

namespace Singularity.UI.Views;

public sealed class ReportsView : Panel
{
	private readonly FlowLayoutPanel historyList = new();
	private readonly Panel previewPanel = new();
	private readonly TableLayoutPanel previewGrid = new();
	private readonly Label emptyPreviewLabel = new();
	private readonly Label overallValue = CreateValueLabel();
	private readonly Label profileValue = CreateValueLabel();
	private readonly Label modeValue = CreateValueLabel();
	private readonly Label startedValue = CreateValueLabel();
	private readonly Label finishedValue = CreateValueLabel();
	private readonly Label durationValue = CreateValueLabel();
	private readonly Label cpuValue = CreateValueLabel();
	private readonly Label memoryValue = CreateValueLabel();
	private readonly Label gpuValue = CreateValueLabel();
	private readonly Label statisticsValue = CreateValueLabel();
	private readonly List<CommandButton> historyButtons = [];
	private string historySignature = string.Empty;

	public CommandButton ExportJsonButton { get; } = new();
	public CommandButton ExportHtmlButton { get; } = new();

	public event Action<int>? HistorySelectionRequested;

	public ReportsView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Workspace;
		BuildUi();
		historyList.ClientSizeChanged += (_, _) => UpdateHistoryButtonWidths();
		UpdateState(ReportsWorkspaceSnapshot.Empty);
	}

	public void UpdateState(ReportsWorkspaceSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		RenderHistory(snapshot);
		RenderPreview(snapshot);
		ExportJsonButton.Enabled = snapshot.CanExport;
		ExportHtmlButton.Enabled = snapshot.CanExport;
	}

	public void FocusContext(string? contextId)
	{
		switch (contextId)
		{
			case "export":
				ExportJsonButton.Focus();
				break;
			case "report":
				previewPanel.Focus();
				break;
			default:
				if (historyButtons.Count > 0)
					historyButtons[0].Focus();
				else
					historyList.Focus();
				break;
		}
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
			Text = "Qualification Reports",
			Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Workspace
		};
		Label subtitle = new()
		{
			Dock = DockStyle.Fill,
			Text = "Bounded session history, report review, and export.",
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Workspace
		};
		header.Controls.Add(subtitle);
		header.Controls.Add(title);

		SplitContainer split = new()
		{
			Dock = DockStyle.Fill,
			Orientation = Orientation.Vertical,
			SplitterWidth = ThemeMetrics.SplitterWidth,
			SplitterDistance = 260,
			Panel1MinSize = 180,
			Panel2MinSize = 240,
			BackColor = Theme.Separator
		};
		split.Panel1.Padding = new Padding(ThemeMetrics.SpacingLarge, ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing, ThemeMetrics.SpacingLarge);
		split.Panel2.Padding = new Padding(ThemeMetrics.Spacing, ThemeMetrics.SpacingLarge, ThemeMetrics.SpacingLarge, ThemeMetrics.SpacingLarge);

		split.Panel1.Controls.Add(BuildHistorySurface());
		split.Panel2.Controls.Add(BuildPreviewSurface());

		Controls.Add(split);
		Controls.Add(header);
	}

	private Control BuildHistorySurface()
	{
		Panel surface = new()
		{
			Dock = DockStyle.Fill,
			BackColor = Theme.Panel,
			Padding = new Padding(ThemeMetrics.Spacing)
		};
		Label title = new()
		{
			Dock = DockStyle.Top,
			Height = 34,
			Text = "HISTORY",
			Font = ThemeFonts.Header,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel
		};
		historyList.Dock = DockStyle.Fill;
		historyList.FlowDirection = FlowDirection.TopDown;
		historyList.WrapContents = false;
		historyList.AutoScroll = true;
		historyList.BackColor = Theme.Panel;
		historyList.TabStop = true;
		surface.Controls.Add(historyList);
		surface.Controls.Add(title);
		return surface;
	}

	private Control BuildPreviewSurface()
	{
		previewPanel.Dock = DockStyle.Fill;
		previewPanel.BackColor = Theme.Panel;
		previewPanel.Padding = new Padding(ThemeMetrics.Spacing);
		previewPanel.TabStop = true;

		Label title = new()
		{
			Dock = DockStyle.Top,
			Height = 34,
			Text = "REPORT",
			Font = ThemeFonts.Header,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel
		};

		FlowLayoutPanel exports = new()
		{
			Dock = DockStyle.Bottom,
			Height = 48,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			BackColor = Theme.Panel,
			Padding = new Padding(0, ThemeMetrics.SpacingSmall, 0, 0)
		};
		ConfigureExportButton(ExportJsonButton, "Export JSON");
		ConfigureExportButton(ExportHtmlButton, "Export HTML");
		exports.Controls.Add(ExportJsonButton);
		exports.Controls.Add(ExportHtmlButton);

		previewGrid.Dock = DockStyle.Top;
		previewGrid.AutoSize = true;
		previewGrid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		previewGrid.ColumnCount = 2;
		previewGrid.RowCount = 10;
		previewGrid.BackColor = Theme.Panel;
		previewGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
		previewGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
		for (int row = 0; row < 10; row++)
			previewGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

		AddRow(previewGrid, 0, "Overall", overallValue);
		AddRow(previewGrid, 1, "Profile", profileValue);
		AddRow(previewGrid, 2, "Mode", modeValue);
		AddRow(previewGrid, 3, "Started", startedValue);
		AddRow(previewGrid, 4, "Finished", finishedValue);
		AddRow(previewGrid, 5, "Duration", durationValue);
		AddRow(previewGrid, 6, "CPU", cpuValue);
		AddRow(previewGrid, 7, "Memory", memoryValue);
		AddRow(previewGrid, 8, "GPU", gpuValue);
		AddRow(previewGrid, 9, "Telemetry", statisticsValue);

		emptyPreviewLabel.Dock = DockStyle.Fill;
		emptyPreviewLabel.Text = "Select a qualification history entry to review its report evidence.";
		emptyPreviewLabel.Font = ThemeFonts.Subtitle;
		emptyPreviewLabel.ForeColor = Theme.TextMuted;
		emptyPreviewLabel.BackColor = Theme.Panel;
		emptyPreviewLabel.TextAlign = ContentAlignment.MiddleCenter;

		previewPanel.Controls.Add(emptyPreviewLabel);
		previewPanel.Controls.Add(previewGrid);
		previewPanel.Controls.Add(exports);
		previewPanel.Controls.Add(title);
		previewGrid.BringToFront();
		return previewPanel;
	}

	private void RenderHistory(ReportsWorkspaceSnapshot snapshot)
	{
		string signature = string.Join(
			"|",
			snapshot.Records.Select(record =>
				$"{record.StartedAt.Ticks}:{record.Result}:{record.Report is not null}"));

		if (!string.Equals(signature, historySignature, StringComparison.Ordinal))
		{
			historySignature = signature;
			historyButtons.Clear();
			historyList.Controls.Clear();

			if (snapshot.Records.Count == 0)
			{
				historyList.Controls.Add(new Label
				{
					AutoSize = true,
					Text = "No qualification history is available.",
					Font = ThemeFonts.CardText,
					ForeColor = Theme.TextMuted,
					BackColor = Theme.Panel,
					Margin = new Padding(0, ThemeMetrics.Spacing, 0, 0)
				});
			}
			else
			{
				for (int index = 0; index < snapshot.Records.Count; index++)
				AddHistoryButton(snapshot.Records[index], index);
			}
		}

		for (int index = 0; index < historyButtons.Count; index++)
		{
			bool selected = index == snapshot.SelectedIndex;
			historyButtons[index].BackColor = selected ? Theme.Selected : Theme.PanelLight;
			historyButtons[index].FlatAppearance.BorderSize = selected ? 1 : 0;
			historyButtons[index].FlatAppearance.BorderColor = selected ? Theme.Accent : Theme.PanelLight;
		}
	}

	private void AddHistoryButton(QualificationRecord record, int index)
	{
		CommandButton button = new()
		{
			Width = Math.Max(180, historyList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - ThemeMetrics.Spacing),
			Height = 58,
			Margin = new Padding(0, 0, 0, ThemeMetrics.SpacingSmall),
			TextAlign = ContentAlignment.MiddleLeft,
			AutoEllipsis = true,
			Text = $"{record.DisplayResult}  •  {record.ProfileName}\r\n{record.StartedAt:g}  •  {record.DisplayDuration}",
			AccessibleName = $"Qualification {record.DisplayResult}, {record.StartedAt:g}"
		};
		button.Click += (_, _) => HistorySelectionRequested?.Invoke(index);
		historyButtons.Add(button);
		historyList.Controls.Add(button);
	}

	private void UpdateHistoryButtonWidths()
	{
		int width = Math.Max(
			180,
			historyList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - ThemeMetrics.Spacing);

		foreach (CommandButton button in historyButtons)
			button.Width = width;
	}

	private void RenderPreview(ReportsWorkspaceSnapshot snapshot)
	{
		bool hasRecord = snapshot.SelectedRecord is not null;
		emptyPreviewLabel.Visible = !hasRecord;
		previewGrid.Visible = hasRecord;

		if (snapshot.SelectedRecord is not { } record)
		{
			ResetPreview();
			return;
		}

		SetStatus(overallValue, record.Result);
		profileValue.Text = string.IsNullOrWhiteSpace(record.ProfileName) ? "Unavailable" : record.ProfileName;
		modeValue.Text = record.ExecutionMode == QualificationExecutionMode.Unknown
			? "Unavailable"
			: record.ExecutionMode.ToString().ToUpperInvariant();
		startedValue.Text = record.StartedAt.ToString("G");
		finishedValue.Text = record.FinishedAt.ToString("G");
		durationValue.Text = record.Duration.ToString(@"hh\:mm\:ss");

		if (snapshot.SelectedReport is { } report)
		{
			SetStatus(cpuValue, report.CpuResult);
			SetStatus(memoryValue, report.MemoryResult);
			SetStatus(gpuValue, report.GpuResult);
			statisticsValue.Text = BuildStatisticsSummary(report.TelemetryStatistics);
		}
		else
		{
			SetStatus(cpuValue, ValidationStatus.Unknown);
			SetStatus(memoryValue, ValidationStatus.Unknown);
			SetStatus(gpuValue, ValidationStatus.Unknown);
			statisticsValue.Text = "Unavailable";
		}
	}

	private void ResetPreview()
	{
		SetStatus(overallValue, ValidationStatus.Unknown);
		profileValue.Text = "Unavailable";
		modeValue.Text = "Unavailable";
		startedValue.Text = "Unavailable";
		finishedValue.Text = "Unavailable";
		durationValue.Text = "00:00:00";
		SetStatus(cpuValue, ValidationStatus.Unknown);
		SetStatus(memoryValue, ValidationStatus.Unknown);
		SetStatus(gpuValue, ValidationStatus.Unknown);
		statisticsValue.Text = "Unavailable";
	}

	private static string BuildStatisticsSummary(SessionTelemetryStatistics statistics)
	{
		List<string> values = [];
		if (statistics.CpuLoadPercent is { } cpu)
			values.Add($"CPU {cpu.Average:0}% avg");
		if (statistics.SystemMemoryUsagePercent is { } memory)
			values.Add($"RAM {memory.Average:0}% avg");
		if (statistics.GpuLoadPercent is { } gpu)
			values.Add($"GPU {gpu.Average:0}% avg");

		return values.Count == 0 ? "Unavailable" : string.Join(" • ", values);
	}

	private static void ConfigureExportButton(CommandButton button, string text)
	{
		button.Width = 132;
		button.Height = ThemeMetrics.ControlHeight;
		button.Text = text;
		button.BackColor = Theme.Accent;
		button.ForeColor = Color.Black;
		button.Enabled = false;
		button.Margin = new Padding(0, 0, ThemeMetrics.SpacingSmall, 0);
	}

	private static void AddRow(TableLayoutPanel grid, int row, string label, Label value)
	{
		Label name = new()
		{
			Dock = DockStyle.Fill,
			Text = label,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleLeft
		};
		value.Dock = DockStyle.Fill;
		grid.Controls.Add(name, 0, row);
		grid.Controls.Add(value, 1, row);
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

	private static void SetStatus(Label label, ValidationStatus status)
	{
		label.Text = StatusStyle.Format(status);
		label.ForeColor = StatusStyle.GetColor(status);
	}
}
