// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class ReportSection : Panel
{
	public Button ExportJsonButton { get; } = new();
	public Button ExportHtmlButton { get; } = new();
	private readonly ValueRow startedRow;
	private readonly ValueRow finishedRow;
	private readonly ValueRow durationRow;
	private readonly ValueRow cpuRow;
	private readonly ValueRow memoryRow;
	private readonly ValueRow gpuRow;
	private readonly ValueRow overallRow;
	private readonly ValueRow profileRow;
	private readonly ValueRow cpuLoadRow;
	private readonly ValueRow gpuLoadRow;
	private readonly ValueRow gpuTemperatureRow;
	private readonly ValueRow gpuPowerRow;
	private readonly ValueRow gpuVramRow;
	private readonly ValueRow systemMemoryRow;

	public ReportSection()
	{
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 480;
		BackColor = Theme.Panel;

		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"REPORT");

		startedRow = CreateRow("Started", 55);
		finishedRow = CreateRow("Finished", 80);
		durationRow = CreateRow("Duration", 105);
		cpuRow = CreateRow("CPU", 130);
		memoryRow = CreateRow("Memory", 155);
		gpuRow = CreateRow("GPU", 180);
		overallRow = CreateRow("Overall", 205);
		profileRow = CreateRow("Profile", 240);
		cpuLoadRow = CreateRow("CPU avg / max", 265);
		gpuLoadRow = CreateRow("GPU avg / max", 290);
		gpuTemperatureRow = CreateRow("GPU temp", 315);
		gpuPowerRow = CreateRow("GPU power", 340);
		gpuVramRow = CreateRow("VRAM avg / max", 365);
		systemMemoryRow = CreateRow("RAM avg / max", 390);
		int exportWidth = (Width - LayoutConstants.SectionPadding * 2 - LayoutConstants.CardGap) / 2;
		ConfigureExportButton(ExportJsonButton, "EXPORT JSON", LayoutConstants.SectionPadding, exportWidth);
		ConfigureExportButton(ExportHtmlButton, "EXPORT HTML", LayoutConstants.SectionPadding + exportWidth + LayoutConstants.CardGap, exportWidth);

		Controls.AddRange([
			startedRow,
			finishedRow,
			durationRow,
			cpuRow,
			memoryRow,
			gpuRow,
			overallRow,
			profileRow,
			cpuLoadRow,
			gpuLoadRow,
			gpuTemperatureRow,
			gpuPowerRow,
			gpuVramRow,
			systemMemoryRow,
			ExportJsonButton,
			ExportHtmlButton
		]);
	}

	public void UpdateReport(QualificationReport report)
	{
		startedRow.SetValue(report.StartedAt.ToString("HH:mm:ss"));
		finishedRow.SetValue(report.FinishedAt.ToString("HH:mm:ss"));
		durationRow.SetValue(report.Duration.ToString(@"hh\:mm\:ss"));
		SetStatus(cpuRow, report.CpuResult);
		SetStatus(memoryRow, report.MemoryResult);
		SetStatus(gpuRow, report.GpuResult);
		SetStatus(overallRow, report.OverallResult);
		profileRow.SetValue(report.Profile.Name);

		SessionTelemetryStatistics statistics = report.TelemetryStatistics;
		cpuLoadRow.SetValue(FormatMetric(statistics.CpuLoadPercent, "%"));
		gpuLoadRow.SetValue(FormatMetric(statistics.GpuLoadPercent, "%"));
		gpuTemperatureRow.SetValue(FormatMetric(statistics.GpuTemperatureCelsius, "°C"));
		gpuPowerRow.SetValue(FormatMetric(statistics.GpuPowerWatts, "W"));
		gpuVramRow.SetValue(FormatMetric(statistics.GpuVramUsagePercent, "%"));
		systemMemoryRow.SetValue(FormatMetric(statistics.SystemMemoryUsagePercent, "%"));
		ExportJsonButton.Enabled = true;
		ExportHtmlButton.Enabled = true;
	}

	public void Reset()
	{
		startedRow.SetValue("-");
		finishedRow.SetValue("-");
		durationRow.SetValue("-");
		cpuRow.SetValue("-", Theme.TextMain);
		memoryRow.SetValue("-", Theme.TextMain);
		gpuRow.SetValue("-", Theme.TextMain);
		overallRow.SetValue("-", Theme.TextMain);
		profileRow.SetValue("-");
		cpuLoadRow.SetValue("-");
		gpuLoadRow.SetValue("-");
		gpuTemperatureRow.SetValue("-");
		gpuPowerRow.SetValue("-");
		gpuVramRow.SetValue("-");
		systemMemoryRow.SetValue("-");
		ExportJsonButton.Enabled = false;
		ExportHtmlButton.Enabled = false;
	}

	private static string FormatMetric(MetricStatistics? statistics, string unit)
	{
		return statistics is null
			? "N/A"
			: $"{statistics.Average:0.0} / {statistics.Maximum:0.0} {unit}";
	}

	private static void SetStatus(ValueRow row, ValidationStatus status)
	{
		row.SetValue(StatusStyle.Format(status), StatusStyle.GetColor(status));
	}

	private static ValueRow CreateRow(string title, int top)
	{
		return new ValueRow(title, "-")
		{
			Left = LayoutConstants.SectionPadding,
			Top = top
		};
	}

	private static void ConfigureExportButton(Button button, string text, int left, int width)
	{
		button.SetBounds(left, 425, width, 36);
		button.Text = text;
		button.Enabled = false;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.BackColor = Theme.Accent;
		button.ForeColor = Color.Black;
		button.Font = ThemeFonts.Button;
	}

}
