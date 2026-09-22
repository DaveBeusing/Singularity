// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.UI.Layout;
using Singularity.UI.Sections;

namespace Singularity.UI.Views;

public sealed class ReportsView : Panel
{
	private readonly HistorySection historySection;
	private readonly ReportSection reportSection;

	public ReportsView()
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.MainWidth;
		BackColor = Theme.Background;

		reportSection = new ReportSection
		{
			Left = 0,
			Top = 0
		};

		historySection = new HistorySection(
			LayoutConstants.MetricsPanelWidth,
			reportSection.Height)
		{
			Left = LayoutConstants.SidePanelLeft,
			Top = 0
		};

		Controls.AddRange([
			reportSection,
			historySection
		]);

		Height = Math.Max(reportSection.Bottom, historySection.Bottom);
	}

	public Button ExportJsonButton => reportSection.ExportJsonButton;
	public Button ExportHtmlButton => reportSection.ExportHtmlButton;

	public void UpdateHistory(QualificationHistory history)
	{
		historySection.UpdateHistory(history);
	}

	public void UpdateReport(QualificationReport report)
	{
		reportSection.UpdateReport(report);
	}

	public void ResetReport()
	{
		reportSection.Reset();
	}
}
