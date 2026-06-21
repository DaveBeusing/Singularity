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
	private readonly Label startedValue;
	private readonly Label finishedValue;
	private readonly Label durationValue;
	private readonly Label cpuValue;
	private readonly Label memoryValue;
	private readonly Label overallValue;

	public ReportSection()
	{
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 220;
		BackColor = Theme.Panel;

		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"REPORT");

		Controls.Add(CreateTitleLabel("Started", 55));
		Controls.Add(CreateTitleLabel("Finished", 80));
		Controls.Add(CreateTitleLabel("Duration", 105));
		Controls.Add(CreateTitleLabel("CPU", 130));
		Controls.Add(CreateTitleLabel("Memory", 155));
		Controls.Add(CreateTitleLabel("Overall", 180));

		startedValue = CreateValueLabel("-", 55);
		finishedValue = CreateValueLabel("-", 80);
		durationValue = CreateValueLabel("-", 105);
		cpuValue = CreateValueLabel("-", 130);
		memoryValue = CreateValueLabel("-", 155);
		overallValue = CreateValueLabel("-", 180);

		Controls.AddRange([
			startedValue,
			finishedValue,
			durationValue,
			cpuValue,
			memoryValue,
			overallValue
		]);
	}

	public void UpdateReport(QualificationReport report)
	{
		startedValue.Text = report.StartedAt.ToString("HH:mm:ss");
		finishedValue.Text = report.FinishedAt.ToString("HH:mm:ss");
		durationValue.Text = report.Duration.ToString(@"hh\:mm\:ss");

		cpuValue.Text = FormatStatus(report.CpuResult);
		memoryValue.Text = FormatStatus(report.MemoryResult);
		overallValue.Text = FormatStatus(report.OverallResult);

		cpuValue.ForeColor = GetStatusColor(report.CpuResult);
		memoryValue.ForeColor = GetStatusColor(report.MemoryResult);
		overallValue.ForeColor = GetStatusColor(report.OverallResult);
	}

	public void Reset()
	{
		startedValue.Text = "-";
		finishedValue.Text = "-";
		durationValue.Text = "-";
		cpuValue.Text = "-";
		memoryValue.Text = "-";
		overallValue.Text = "-";

		cpuValue.ForeColor = Theme.TextMain;
		memoryValue.ForeColor = Theme.TextMain;
		overallValue.ForeColor = Theme.TextMain;
	}

	private static string FormatStatus(ValidationStatus status)
	{
		return status.ToString().ToUpperInvariant();
	}

	private static Color GetStatusColor(ValidationStatus status)
	{
		return status switch
		{
			ValidationStatus.Pass => Theme.Success,
			ValidationStatus.Warning => Theme.Accent,
			ValidationStatus.Fail => Theme.Danger,
			_ => Theme.TextMuted
		};
	}

	private static Label CreateTitleLabel(string text, int top)
	{
		return new Label
		{
			Text = text,
			Left = 20,
			Top = top,
			Width = 100,
			Height = 20,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel
		};
	}

	private static Label CreateValueLabel(string text, int top)
	{
		return new Label
		{
			Text = text,
			Left = 140,
			Top = top,
			Width = 220,
			Height = 20,
			TextAlign = ContentAlignment.MiddleRight,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel
		};
	}

}