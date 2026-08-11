// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Monitoring.Models;
using Singularity.Monitoring.Runtime;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI;

public sealed class MainForm : Form
{
	private const string VersionString = "v0.24.0-alpha";

	private readonly QualificationCoordinator coordinator;
	private readonly ReportExportService reportExportService;
	private readonly SystemMonitor systemMonitor;
	private readonly System.Windows.Forms.Timer timer = new();

	private readonly Button hardwareTabButton = new();
	private readonly Button workloadsTabButton = new();

	private readonly Panel tabBarPanel = new();
	private readonly Panel tabHostPanel = new();

	private readonly Label statusBadge = new();

	private HardwareView hardwareView = null!;
	private WorkloadsView workloadsView = null!;

	private enum ActiveTab
	{
		Hardware,
		Workloads
	}

	private ActiveTab activeTab = ActiveTab.Hardware;

	public MainForm(
		QualificationCoordinator coordinator,
		ReportExportService reportExportService,
		SystemMonitor systemMonitor)
	{
		this.coordinator = coordinator;
		this.reportExportService = reportExportService;
		this.systemMonitor = systemMonitor;
		Text = "//Singularity✦";
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		BackColor = Theme.Background;
		ForeColor = Theme.TextMain;
		Font = ThemeFonts.Title;
		AutoScaleMode = AutoScaleMode.Dpi;

		BuildUi();

		timer.Interval = 500;
		timer.Tick += (_, _) => UpdateMonitoring();
		timer.Start();
	}

	private void BuildUi()
	{
		Controls.Clear();

		Label title = new()
		{
			Text = "//Singularity✦",
			Left = LayoutConstants.HeaderLeft,
			Top = LayoutConstants.HeaderTop,
			Width = 490,
			Height = 64,
			Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Background
		};

		Label subtitle = new()
		{
			Text = "Platform Qualification Suite",
			Left = LayoutConstants.HeaderLeft + 2,
			Top = 80,
			Width = 520,
			Height = 28,
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label versionLabel = new()
		{
			Text = VersionString,
			Left = 750,
			Top = 32,
			Width = 130,
			Height = 24,
			Font = ThemeFonts.SectionHeader,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.MiddleRight
		};

		ConfigureStatusBadge();
		BuildTabs();
		BuildViews();

		Controls.AddRange([
			title,
			subtitle,
			versionLabel,
			statusBadge,
			tabBarPanel,
			tabHostPanel
		]);

		workloadsView.StartButton.Click += (_, _) => StartWorkloads();
		workloadsView.AutoButton.Click += (_, _) => StartAutomatedQualification();
		workloadsView.StopButton.Click += (_, _) => StopWorkloads();
		workloadsView.ExportJsonButton.Click += (_, _) => ExportJsonReport();
		workloadsView.ExportHtmlButton.Click += (_, _) => ExportHtmlReport();

		SwitchTab(ActiveTab.Hardware);
		UpdateWorkloadStatus();
		workloadsView.UpdateSession(coordinator.Session);
		workloadsView.UpdateHistory(coordinator.History);
		workloadsView.ResetReport();

		ClientSize = new Size(
			LayoutConstants.WindowWidth,
			tabHostPanel.Bottom + LayoutConstants.SectionGap);

		MinimumSize = Size;
		MaximumSize = Size;
	}

	private void ConfigureStatusBadge()
	{
		statusBadge.Left = 750;
		statusBadge.Top = 78;
		statusBadge.Width = 130;
		statusBadge.Height = 32;
		statusBadge.Text = "READY";
		statusBadge.TextAlign = ContentAlignment.MiddleCenter;
		statusBadge.Font = new Font("Segoe UI", 9, FontStyle.Bold);
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;
	}

	private void BuildTabs()
	{
		tabBarPanel.Left = LayoutConstants.MainLeft;
		tabBarPanel.Top = 140;
		tabBarPanel.Width = LayoutConstants.MainWidth;
		tabBarPanel.Height = LayoutConstants.TabBarHeight;
		tabBarPanel.BackColor = Theme.Panel;

		ConfigureTabButton(
			hardwareTabButton,
			"PLATFORM",
			20,
			ActiveTab.Hardware);

		ConfigureTabButton(
			workloadsTabButton,
			"WORKLOADS",
			250,
			ActiveTab.Workloads);

		tabBarPanel.Controls.AddRange([
			hardwareTabButton,
			workloadsTabButton
		]);
	}

	private void BuildViews()
	{
		hardwareView = new HardwareView
		{
			Left = 0,
			Top = 0
		};

		workloadsView = new WorkloadsView
		{
			Left = 0,
			Top = 0
		};

		int contentHeight = Math.Max(
			hardwareView.Height,
			workloadsView.Height);

		tabHostPanel.Left = LayoutConstants.MainLeft;
		tabHostPanel.Top = tabBarPanel.Bottom + LayoutConstants.SectionGap;
		tabHostPanel.Width = LayoutConstants.MainWidth;
		tabHostPanel.Height = contentHeight;
		tabHostPanel.AutoScroll = true;
		tabHostPanel.BackColor = Theme.Background;

		tabHostPanel.Controls.AddRange([
			hardwareView,
			workloadsView
		]);
	}

	private void ConfigureTabButton(
		Button button,
		string text,
		int left,
		ActiveTab tab)
	{
		button.Text = text;
		button.Left = left;
		button.Top = 10;
		button.Width = tab == ActiveTab.Hardware ? 220 : 240;
		button.Height = 34;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.Font = ThemeFonts.Button;
		button.Click += (_, _) => SwitchTab(tab);
	}

	private void SwitchTab(ActiveTab tab)
	{
		activeTab = tab;

		hardwareView.Visible = activeTab == ActiveTab.Hardware;
		workloadsView.Visible = activeTab == ActiveTab.Workloads;

		hardwareTabButton.BackColor =
			activeTab == ActiveTab.Hardware
				? Theme.Accent
				: Theme.PanelLight;

		hardwareTabButton.ForeColor =
			activeTab == ActiveTab.Hardware
				? Color.Black
				: Theme.TextMain;

		workloadsTabButton.BackColor =
			activeTab == ActiveTab.Workloads
				? Theme.Accent
				: Theme.PanelLight;

		workloadsTabButton.ForeColor =
			activeTab == ActiveTab.Workloads
				? Color.Black
				: Theme.TextMain;
	}

	private void StartWorkloads()
	{
		if (coordinator.StartManual(workloadsView.CreateOptions(), workloadsView.SelectedProfile))
		{
			RenderQualificationState();
			SwitchTab(ActiveTab.Workloads);
		}
	}

	private void StartAutomatedQualification()
	{
		try
		{
			if (coordinator.StartAutomated(workloadsView.CreateOptions(), workloadsView.SelectedProfile))
			{
				RenderQualificationState();
				SwitchTab(ActiveTab.Workloads);
			}
		}
		catch (InvalidOperationException ex)
		{
			MessageBox.Show(this, ex.Message, "Automated qualification", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}

	private void StopWorkloads()
	{
		if (coordinator.Stop())
			RenderQualificationState();
	}

	private void ExportJsonReport()
	{
		if (coordinator.LastReport is not { } report)
			return;

		using SaveFileDialog dialog = new()
		{
			Title = "Export qualification report",
			Filter = "JSON report (*.json)|*.json|All files (*.*)|*.*",
			DefaultExt = "json",
			AddExtension = true,
			FileName = $"singularity-report-{report.FinishedAt:yyyyMMdd-HHmmss}.json"
		};

		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;

		try
		{
			reportExportService.ExportJson(dialog.FileName, report, hardwareView.Inventory, VersionString);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			MessageBox.Show(this, ex.Message, "Report export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void ExportHtmlReport()
	{
		if (coordinator.LastReport is not { } report)
			return;

		using SaveFileDialog dialog = new()
		{
			Title = "Export qualification report",
			Filter = "HTML report (*.html)|*.html|All files (*.*)|*.*",
			DefaultExt = "html",
			AddExtension = true,
			FileName = $"singularity-report-{report.FinishedAt:yyyyMMdd-HHmmss}.html"
		};

		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;

		try
		{
			reportExportService.ExportHtml(dialog.FileName, report, hardwareView.Inventory, VersionString);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			MessageBox.Show(this, ex.Message, "Report export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void UpdateMonitoring()
	{
		SystemSnapshot snapshot = systemMonitor.GetSnapshot();

		workloadsView.UpdateMetrics(snapshot);
		coordinator.Update(snapshot);
		RenderQualificationState();
	}

	private void RenderQualificationState()
	{
		if (coordinator.LastValidationResult is { } validation)
			workloadsView.UpdateValidation(validation);
		else
			workloadsView.ResetValidation();

		if (coordinator.LastReport is { } report)
			workloadsView.UpdateReport(report);
		else
			workloadsView.ResetReport();

		workloadsView.UpdateQualificationProgress(coordinator.Progress);
		workloadsView.UpdateSession(coordinator.Session);
		workloadsView.UpdateHistory(coordinator.History);
		UpdateWorkloadStatus();
	}

	private void UpdateWorkloadStatus()
	{
		WorkloadStatus status = coordinator.WorkloadStatus;

		ValidationSummary? validationSummary =
			coordinator.LastValidationResult is not null
				? new ValidationSummary(coordinator.LastValidationResult)
				: null;

		string badgeText = status.State switch
		{
			WorkloadState.Stopped => "READY",
			WorkloadState.Starting => "STARTING",
			WorkloadState.Running => "RUNNING",
			WorkloadState.Stopping => "STOPPING",
			WorkloadState.Failed => "FAILED",
			_ => "UNKNOWN"
		};

		Color badgeBackColor = status.State switch
		{
			WorkloadState.Stopped => Theme.PanelLight,
			WorkloadState.Starting => Theme.Accent,
			WorkloadState.Running => Theme.Success,
			WorkloadState.Stopping => Theme.Accent,
			WorkloadState.Failed => Theme.Danger,
			_ => Theme.PanelLight
		};

		Color badgeForeColor = status.State switch
		{
			WorkloadState.Starting => Color.Black,
			WorkloadState.Stopping => Color.Black,
			_ => Theme.TextMain
		};

		if (status.State == WorkloadState.Running &&
			validationSummary is not null)
		{
			switch (validationSummary.OverallStatus)
			{
				case ValidationStatus.Pass:
					badgeBackColor = Theme.Success;
					break;

				case ValidationStatus.Warning:
					badgeBackColor = Theme.Accent;
					badgeForeColor = Color.Black;
					break;

				case ValidationStatus.Fail:
					badgeBackColor = Theme.Danger;
					break;
			}
		}

		ControlUpdate.SetText(statusBadge, badgeText);
		ControlUpdate.SetBackColor(statusBadge, badgeBackColor);
		ControlUpdate.SetForeColor(statusBadge, badgeForeColor);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			timer.Dispose();
		}

		base.Dispose(disposing);
	}

}
