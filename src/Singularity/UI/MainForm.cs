// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Application.Commands;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Monitoring.Models;
using Singularity.Monitoring.Runtime;
using Singularity.UI.Controls;
using Singularity.UI.Navigation;
using Singularity.UI.Shell;
using Singularity.UI.Views;

namespace Singularity.UI;

public sealed class MainForm : Form
{
	private readonly QualificationCoordinator coordinator;
	private readonly ReportExportService reportExportService;
	private readonly SystemMonitor systemMonitor;
	private readonly NavigationService navigationService = new(WorkspaceCatalog.CreateDefault());
	private readonly CommandRouter commandRouter = new();
	private readonly List<ButtonCommandBinding> commandBindings = [];
	private readonly System.Windows.Forms.Timer timer = new();
	private readonly CancellationTokenSource shutdownCancellation = new();
	private Icon? applicationIcon;
	private bool inventoryRefreshInProgress;

	private ApplicationShell shell = null!;
	private HardwareView hardwareView = null!;
	private QualificationView qualificationView = null!;
	private ResultsView resultsView = null!;
	private ReportsView reportsView = null!;

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
		RegisterApplicationCommands();
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

	private void RegisterApplicationCommands()
	{
		commandRouter.Register(
			CommandId.StartQualification,
			StartWorkloads,
			() => coordinator.WorkloadStatus.State is WorkloadState.Stopped or WorkloadState.Failed);

		commandRouter.Register(
			CommandId.AutomatedQualification,
			StartAutomatedQualification,
			() => coordinator.WorkloadStatus.State is WorkloadState.Stopped or WorkloadState.Failed);

		commandRouter.Register(
			CommandId.StopQualification,
			StopWorkloads,
			() => coordinator.WorkloadStatus.State is WorkloadState.Starting or WorkloadState.Running);

		commandRouter.Register(
			CommandId.ExportJson,
			ExportJsonReport,
			() => coordinator.LastReport is not null);

		commandRouter.Register(
			CommandId.ExportHtml,
			ExportHtmlReport,
			() => coordinator.LastReport is not null);

		commandRouter.Register(
			CommandId.RefreshInventory,
			RefreshInventory,
			() =>
				navigationService.ActiveWorkspace == WorkspaceId.Platform &&
				coordinator.WorkloadStatus.State is WorkloadState.Stopped or WorkloadState.Failed &&
				!inventoryRefreshInProgress);
	}

	private void BuildUi()
	{
		SuspendLayout();
		try
		{
			Controls.Clear();

			shell = new ApplicationShell(
				ApplicationMetadata.Version,
				navigationService,
				commandRouter)
			{
				Dock = DockStyle.Fill
			};

			hardwareView = new HardwareView();
			qualificationView = new QualificationView();
			resultsView = new ResultsView();
			reportsView = new ReportsView();

			shell.RegisterWorkspace(
				WorkspaceId.Overview,
				new WorkspacePlaceholderView(
					"Overview",
					"Use this workspace as the platform qualification entry point. Detailed overview content will be migrated in a later workspace package."));
			shell.RegisterWorkspace(WorkspaceId.Platform, hardwareView);
			shell.RegisterWorkspace(WorkspaceId.Qualification, qualificationView);
			shell.RegisterWorkspace(WorkspaceId.Results, resultsView);
			shell.RegisterWorkspace(WorkspaceId.Reports, reportsView);
			shell.RegisterWorkspace(
				WorkspaceId.Settings,
				new WorkspacePlaceholderView(
					"Settings",
					"Application settings are prepared as a dedicated workspace. Domain-specific settings will be migrated when their ownership is defined."));

			Controls.Add(shell);
			BindCommandButtons();

			UpdateWorkloadStatus();
			resultsView.UpdateSession(coordinator.Session);
			reportsView.UpdateHistory(coordinator.History);
			reportsView.ResetReport();
			commandRouter.RefreshStates();
		}
		finally
		{
			ResumeLayout(true);
		}
	}

	private void BindCommandButtons()
	{
		foreach (ButtonCommandBinding binding in commandBindings)
			binding.Dispose();

		commandBindings.Clear();
		commandBindings.Add(new ButtonCommandBinding(qualificationView.StartButton, commandRouter, CommandId.StartQualification));
		commandBindings.Add(new ButtonCommandBinding(qualificationView.AutoButton, commandRouter, CommandId.AutomatedQualification));
		commandBindings.Add(new ButtonCommandBinding(qualificationView.StopButton, commandRouter, CommandId.StopQualification));
		commandBindings.Add(new ButtonCommandBinding(reportsView.ExportJsonButton, commandRouter, CommandId.ExportJson));
		commandBindings.Add(new ButtonCommandBinding(reportsView.ExportHtmlButton, commandRouter, CommandId.ExportHtml));
	}

	private void StartWorkloads()
	{
		if (coordinator.StartManual(qualificationView.CreateOptions(), qualificationView.SelectedProfile))
		{
			RenderQualificationState();
			navigationService.Navigate(WorkspaceId.Qualification);
		}
	}

	private void StartAutomatedQualification()
	{
		try
		{
			if (coordinator.StartAutomated(qualificationView.CreateOptions(), qualificationView.SelectedProfile))
			{
				RenderQualificationState();
				navigationService.Navigate(WorkspaceId.Qualification);
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

	private async void RefreshInventory()
	{
		if (inventoryRefreshInProgress)
			return;

		inventoryRefreshInProgress = true;
		commandRouter.RefreshStates();

		try
		{
			await hardwareView.RefreshInventoryAsync(shutdownCancellation.Token);
		}
		catch (OperationCanceledException) when (shutdownCancellation.IsCancellationRequested)
		{
			return;
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Inventory refresh failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		finally
		{
			inventoryRefreshInProgress = false;
			if (!IsDisposed)
				commandRouter.RefreshStates();
		}
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

		qualificationView.UpdateMetrics(snapshot);
		coordinator.Update(snapshot);
		RenderQualificationState();
	}

	private void RenderQualificationState()
	{
		if (coordinator.LastValidationResult is { } validation)
			resultsView.UpdateValidation(validation);
		else
			resultsView.ResetValidation();

		if (coordinator.LastReport is { } report)
			reportsView.UpdateReport(report);
		else
			reportsView.ResetReport();

		qualificationView.UpdateQualificationProgress(coordinator.Progress);
		resultsView.UpdateSession(coordinator.Session);
		reportsView.UpdateHistory(coordinator.History);
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
		commandRouter.RefreshStates();
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (shell.HandleShortcut(keyData))
			return true;

		return base.ProcessCmdKey(ref msg, keyData);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			shutdownCancellation.Cancel();

			foreach (ButtonCommandBinding binding in commandBindings)
				binding.Dispose();

			timer.Dispose();
			shutdownCancellation.Dispose();
			applicationIcon?.Dispose();
		}

		base.Dispose(disposing);
	}
}
