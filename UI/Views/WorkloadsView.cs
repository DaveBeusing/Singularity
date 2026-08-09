// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;
using Singularity.Core.Qualification;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Monitoring;
using Singularity.UI.Layout;
using Singularity.UI.Sections;

namespace Singularity.UI.Views;

public sealed class WorkloadsView : Panel
{
	private const int SubviewNavigationHeight = LayoutConstants.WorkloadsSubviewNavigationHeight;
	private const int SubviewContentTop = SubviewNavigationHeight + LayoutConstants.SectionGap;

	private readonly Button runButton = new();
	private readonly Button resultsButton = new();
	private readonly Button historyButton = new();
	private readonly Panel subviewNavigation = new();
	private readonly Panel runView = new();
	private readonly Panel resultsView = new();
	private readonly Panel historyView = new();

	private WorkloadSection workloadSection = null!;
	private MonitoringSection monitoringSection = null!;
	private ControlSection controlSection = null!;
	private SessionSection sessionSection = null!;
	private ValidationSection validationSection = null!;
	private HistorySection historySection = null!;
	private ReportSection reportSection = null!;

	private enum ActiveSubview
	{
		Run,
		Results,
		History
	}

	public Button StartButton => controlSection.StartButton;
	public Button AutoButton => controlSection.AutoButton;
	public Button StopButton => controlSection.StopButton;
	public QualificationProfile SelectedProfile => workloadSection.SelectedProfile;

	public WorkloadsView()
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.MainWidth;
		BackColor = Theme.Background;

		BuildUi();
	}

	public WorkloadOptions CreateOptions()
	{
		return new WorkloadOptions
		{
			EnableCpuWorkload = workloadSection.CpuCheck.Checked,
			EnableMemoryWorkload = workloadSection.MemoryCheck.Checked,
			EnableGpuWorkload = workloadSection.GpuCheck.Checked,
			CpuThreads = workloadSection.CpuThreadsInput.Value,
			MemoryGb = workloadSection.MemoryGbInput.Value,
			GpuLoadPercent = workloadSection.GpuLoadInput.Value
		};
	}

	public void UpdateMetrics(SystemSnapshot snapshot)
	{
		monitoringSection.UpdateMetrics(snapshot);
	}

	public void UpdateValidation(ValidationResult result)
	{
		validationSection.UpdateValidation(result);
	}

	public void ResetValidation()
	{
		validationSection.Reset();
	}

	public void UpdateSession(QualificationSession session)
	{
		sessionSection.UpdateSession(session);
	}

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

	private void BuildUi()
	{
		Controls.Clear();
		BuildSubviewNavigation();

		workloadSection = new WorkloadSection
		{
			Left = 0,
			Top = 0
		};

		monitoringSection = new MonitoringSection
		{
			Left = LayoutConstants.SidePanelLeft,
			Top = 0
		};

		controlSection = new ControlSection
		{
			Left = LayoutConstants.SidePanelLeft,
			Top = monitoringSection.Bottom + LayoutConstants.SectionGap
		};

		sessionSection = new SessionSection
		{
			Left = 0,
			Top = 0
		};

		validationSection = new ValidationSection
		{
			Left = 0,
			Top = sessionSection.Bottom + LayoutConstants.SectionGap
		};

		historySection = new HistorySection(
			LayoutConstants.MainWidth,
			LayoutConstants.WorkloadsHistoryHeight)
		{
			Left = 0,
			Top = 0
		};

		reportSection = new ReportSection
		{
			Left = LayoutConstants.SidePanelLeft,
			Top = 0
		};

		runView.SetBounds(0, SubviewContentTop, Width, controlSection.Bottom);
		runView.BackColor = Theme.Background;
		runView.Controls.AddRange([
			workloadSection,
			monitoringSection,
			controlSection
		]);

		resultsView.SetBounds(
			0,
			SubviewContentTop,
			Width,
			Math.Max(validationSection.Bottom, reportSection.Bottom));
		resultsView.BackColor = Theme.Background;
		resultsView.Controls.AddRange([
			sessionSection,
			validationSection,
			reportSection
		]);

		historyView.SetBounds(0, SubviewContentTop, Width, historySection.Height);
		historyView.BackColor = Theme.Background;
		historyView.Controls.Add(historySection);

		Controls.AddRange([
			subviewNavigation,
			runView,
			resultsView,
			historyView
		]);

		Height = SubviewContentTop + runView.Height;
		SwitchSubview(ActiveSubview.Run);
	}

	public void UpdateQualificationProgress(QualificationProgress progress)
	{
		string text = progress.State switch
		{
			QualificationRunState.Running => $"{progress.StepNumber}/{progress.StepCount} {progress.StepName}  {progress.Percent:0}%",
			QualificationRunState.Completed => "AUTOMATED RUN COMPLETE",
			QualificationRunState.Cancelled => "AUTOMATED RUN CANCELLED",
			QualificationRunState.Failed => "AUTOMATED RUN FAILED",
			_ => "MANUAL MODE"
		};
		controlSection.UpdateProgress(text);
	}

	private void BuildSubviewNavigation()
	{
		subviewNavigation.SetBounds(0, 0, Width, SubviewNavigationHeight);
		subviewNavigation.BackColor = Theme.Panel;

		ConfigureSubviewButton(runButton, "RUN", 20, ActiveSubview.Run);
		ConfigureSubviewButton(resultsButton, "RESULTS", 160, ActiveSubview.Results);
		ConfigureSubviewButton(historyButton, "HISTORY", 320, ActiveSubview.History);

		subviewNavigation.Controls.AddRange([runButton, resultsButton, historyButton]);
	}

	private void ConfigureSubviewButton(
		Button button,
		string text,
		int left,
		ActiveSubview subview)
	{
		button.Text = text;
		button.SetBounds(left, 8, 130, 32);
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.Font = ThemeFonts.Button;
		button.Click += (_, _) => SwitchSubview(subview);
	}

	private void SwitchSubview(ActiveSubview subview)
	{
		runView.Visible = subview == ActiveSubview.Run;
		resultsView.Visible = subview == ActiveSubview.Results;
		historyView.Visible = subview == ActiveSubview.History;

		StyleSubviewButton(runButton, subview == ActiveSubview.Run);
		StyleSubviewButton(resultsButton, subview == ActiveSubview.Results);
		StyleSubviewButton(historyButton, subview == ActiveSubview.History);
	}

	private static void StyleSubviewButton(Button button, bool active)
	{
		button.BackColor = active ? Theme.Accent : Theme.PanelLight;
		button.ForeColor = active ? Color.Black : Theme.TextMain;
	}

}
