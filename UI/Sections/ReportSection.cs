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
	private readonly ValueRow startedRow;
	private readonly ValueRow finishedRow;
	private readonly ValueRow durationRow;
	private readonly ValueRow cpuRow;
	private readonly ValueRow memoryRow;
	private readonly ValueRow overallRow;

	public ReportSection()
	{
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 220;
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
		overallRow = CreateRow("Overall", 180);

		Controls.AddRange([
			startedRow,
			finishedRow,
			durationRow,
			cpuRow,
			memoryRow,
			overallRow
		]);
	}

	public void UpdateReport(QualificationReport report)
	{
		startedRow.SetValue(report.StartedAt.ToString("HH:mm:ss"));
		finishedRow.SetValue(report.FinishedAt.ToString("HH:mm:ss"));
		durationRow.SetValue(report.Duration.ToString(@"hh\:mm\:ss"));
		SetStatus(cpuRow, report.CpuResult);
		SetStatus(memoryRow, report.MemoryResult);
		SetStatus(overallRow, report.OverallResult);
	}

	public void Reset()
	{
		startedRow.SetValue("-");
		finishedRow.SetValue("-");
		durationRow.SetValue("-");
		cpuRow.SetValue("-", Theme.TextMain);
		memoryRow.SetValue("-", Theme.TextMain);
		overallRow.SetValue("-", Theme.TextMain);
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

}
