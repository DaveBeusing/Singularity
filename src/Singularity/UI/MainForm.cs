// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Monitoring.Models;
using Singularity.Monitoring.Runtime;
using Singularity.UI.Controls;
using Singularity.UI.Shell;
using Singularity.UI.Views;

namespace Singularity.UI;

public sealed class MainForm : Form
{
	private readonly QualificationCoordinator coordinator;
	private readonly ReportExportService reportExportService;
	private readonly SystemMonitor systemMonitor;
	private readonly System.Windows.Forms.Timer timer = new();
	private Icon? applicationIcon;

	private ApplicationShell shell = null!;
	private HardwareView hardwareView = null!;
	private WorkloadsView workloadsView = null!;

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
		FormBorderStyle = FormBorderStyle.Sizable;
		MaximizeBox = true;
		MinimizeBox = true;
		BackColor = Theme.ApplicationBackground;
		ForeColor = Theme.TextMain;
		Font = ThemeFonts.Subtitle;
		AutoScaleMode = AutoScaleMode.Dpi;
		ClientSize = new Size(ThemeMetrics.DefaultWindowWidth, ThemeMetrics.DefaultWindowHeight);
		MinimumSize = new Size(ThemeMetrics.MinimumWindowWidth, ThemeMetrics.MinimumWindowHeight);
		DoubleBuffered = true;

		ConfigureApplicationIcon();
		BuildUi();

		timer.Interval = 500;
		timer.Tick += (_, _) => UpdateMonitoring();
		timer.Start();
	}

	private void ConfigureApplicationIcon()
	{
		try
		{
			applicationIcon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
			if (applicationIcon is not null)
				Icon = applicationIcon;
		}
		catch
		{
			applicationIcon?.Dispose();
			applicationIcon = null;
		}
	}

	private void BuildUi()
	{
		SuspendLayout();
		try
		{
			Controls.Clear();

			shell = new ApplicationShell(ApplicationMetadata.Version)
			{
				Dock = DockStyle.Fill
			};

			hardwareView = new HardwareView();
			workloadsView = new WorkloadsView();

			shell.RegisterWorkspace(ShellSection.Platform, hardwareView);
			shell.RegisterWorkspace(ShellSection.Workloads, workloadsView);
			Controls.Add(shell);

			workloadsView.StartButton.Click += (_, _) => StartWorkloads();
			workloadsView.AutoButton.Click += (_, _) => StartAutomatedQualification();
			workloadsView.StopButton.Click += (_, _) => StopWorkloads();
			workloadsView.ExportJsonButton.Click += (_, _) => ExportJsonReport();
			workloadsView.ExportHtmlButton.Click += (_, _) => ExportHtmlReport();

			shell.ActivateSection(ShellSection.Platform);
			UpdateWorkloadStatus();
			workloadsView.UpdateSession(coordinator.Session);
			workloadsView.UpdateHistory(coordinator.History);
			workloadsView.ResetReport();
		}
		finally
		{
			ResumeLayout(true);
		}
	}

	private void StartWorkloads()
	{
		if (coordinator.StartManual(workloadsView.CreateOptions(), workloadsView.SelectedProfile))
		{
			RenderQualificationState();
			shell.ActivateSection(ShellSection.Workloads);
		}
	}

	private void StartAutomatedQualification()
	{
		try
		{
			if (coordinator.StartAutomated(workloadsView.CreateOptions(), workloadsView.SelectedProfile))
			{
				RenderQualificationState();
				shell.ActivateSection(ShellSection.Workloads);
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
			reportExportService.ExportJson(dialog.FileName, report, hardwareView.Inventory);
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
			reportExportService.ExportHtml(dialog.FileName, report, hardwareView.Inventory);
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

		string statusText = status.State switch
		{
			WorkloadState.Stopped => "READY",
			WorkloadState.Starting => "STARTING",
			WorkloadState.Running => "RUNNING",
			WorkloadState.Stopping => "STOPPING",
			WorkloadState.Failed => "FAILED",
			_ => "UNKNOWN"
		};

		StatusVisualState visualState = status.State switch
		{
			WorkloadState.Stopped => StatusVisualState.Neutral,
			WorkloadState.Starting => StatusVisualState.Warning,
			WorkloadState.Running => StatusVisualState.Success,
			WorkloadState.Stopping => StatusVisualState.Warning,
			WorkloadState.Failed => StatusVisualState.Failure,
			_ => StatusVisualState.Neutral
		};

		if (status.State == WorkloadState.Running && validationSummary is not null)
		{
			visualState = validationSummary.OverallStatus switch
			{
				ValidationStatus.Pass => StatusVisualState.Success,
				ValidationStatus.Warning => StatusVisualState.Warning,
				ValidationStatus.Fail => StatusVisualState.Failure,
				_ => visualState
			};
		}

		shell.SetGlobalStatus(statusText, visualState);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			timer.Dispose();
			applicationIcon?.Dispose();
		}

		base.Dispose(disposing);
	}
}
