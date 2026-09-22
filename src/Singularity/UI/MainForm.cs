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
	private readonly PlatformInventoryState platformInventoryState;
	private readonly NavigationService navigationService = new(WorkspaceCatalog.CreateDefault());
	private readonly CommandRouter commandRouter = new();
	private readonly List<ButtonCommandBinding> commandBindings = [];
	private readonly System.Windows.Forms.Timer timer = new();
	private readonly CancellationTokenSource shutdownCancellation = new();
	private Icon? applicationIcon;

	private ApplicationShell shell = null!;
	private OverviewView overviewView = null!;
	private PlatformView platformView = null!;
	private PlatformInspectorView platformInspectorView = null!;
	private QualificationView qualificationView = null!;
	private ResultsView resultsView = null!;
	private ReportsView reportsView = null!;

	public MainForm(
		QualificationCoordinator coordinator,
		ReportExportService reportExportService,
		SystemMonitor systemMonitor,
		PlatformInventoryState platformInventoryState)
	{
		this.coordinator = coordinator;
		this.reportExportService = reportExportService;
		this.systemMonitor = systemMonitor;
		this.platformInventoryState = platformInventoryState;

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
		Shown += OnShown;

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
			() => coordinator.LastReport is not null && platformInventoryState.Current is not null);

		commandRouter.Register(
			CommandId.ExportHtml,
			ExportHtmlReport,
			() => coordinator.LastReport is not null && platformInventoryState.Current is not null);

		commandRouter.Register(
			CommandId.RefreshInventory,
			RefreshInventory,
			() =>
				navigationService.ActiveWorkspace == WorkspaceId.Platform &&
				coordinator.WorkloadStatus.State is WorkloadState.Stopped or WorkloadState.Failed &&
				!platformInventoryState.IsRefreshing);
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

			overviewView = new OverviewView();
			platformView = new PlatformView();
			platformInspectorView = new PlatformInspectorView();
			qualificationView = new QualificationView();
			resultsView = new ResultsView();
			reportsView = new ReportsView();

			shell.RegisterWorkspace(WorkspaceId.Overview, overviewView);
			shell.RegisterWorkspace(WorkspaceId.Platform, platformView);
			shell.RegisterInspectorContent(WorkspaceId.Platform, platformInspectorView);
			shell.RegisterWorkspace(WorkspaceId.Qualification, qualificationView);
			shell.RegisterWorkspace(WorkspaceId.Results, resultsView);
			shell.RegisterWorkspace(WorkspaceId.Reports, reportsView);
			shell.RegisterWorkspace(
				WorkspaceId.Settings,
				new WorkspacePlaceholderView(
					"Settings",
					"Application settings are prepared as a dedicated workspace. Domain-specific settings will be migrated when their ownership is defined."));

			Controls.Add(shell);

			overviewView.QualificationRequested += OpenQualification;
			platformView.DeviceSelected += OnPlatformDeviceSelected;
			navigationService.ContextItemChanged += OnContextItemChanged;

			BindCommandButtons();
			RenderInventoryState();
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
		commandBindings.Add(new ButtonCommandBinding(platformView.RefreshButton, commandRouter, CommandId.RefreshInventory));
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

	private async void OnShown(object? sender, EventArgs e)
	{
		Shown -= OnShown;
		await RefreshPlatformInventoryAsync();
	}

	private void OpenQualification()
	{
		navigationService.Navigate(WorkspaceId.Qualification);
	}

	private void OnContextItemChanged(NavigationItem? item)
	{
		if (navigationService.ActiveWorkspace != WorkspaceId.Platform)
			return;

		platformView.SetCategory(item?.Id ?? "system");
		platformInspectorView.ShowSelection(null);
	}

	private void OnPlatformDeviceSelected(PlatformDeviceSelection selection)
	{
		navigationService.SetSelection(
			new WorkspaceSelection(
				WorkspaceId.Platform,
				selection.Kind,
				selection.Id,
				selection.DisplayName));

		platformInspectorView.ShowSelection(selection);
		shell.SetInspectorVisible(true);
	}

	private async void RefreshInventory()
	{
		await RefreshPlatformInventoryAsync();
	}

	private async Task RefreshPlatformInventoryAsync()
	{
		if (platformInventoryState.IsRefreshing)
			return;

		Task<bool> refreshTask = platformInventoryState.RefreshAsync(shutdownCancellation.Token);
		RenderInventoryState();
		commandRouter.RefreshStates();

		try
		{
			await refreshTask;
		}
		catch (OperationCanceledException) when (shutdownCancellation.IsCancellationRequested)
		{
			return;
		}
		finally
		{
			if (!IsDisposed)
			{
				RenderInventoryState();
				commandRouter.RefreshStates();
			}
		}
	}

	private void RenderInventoryState()
	{
		overviewView.UpdateInventory(platformInventoryState);
		platformView.UpdateInventory(platformInventoryState);

		if (navigationService.ActiveWorkspace == WorkspaceId.Platform)
		{
			navigationService.SetSelection(null);
			platformInspectorView.ShowSelection(null);
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

		if (platformInventoryState.Current is not { } inventory)
			return;

		try
		{
			reportExportService.ExportJson(dialog.FileName, report, inventory);
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

		if (platformInventoryState.Current is not { } inventory)
			return;

		try
		{
			reportExportService.ExportHtml(dialog.FileName, report, inventory);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			MessageBox.Show(this, ex.Message, "Report export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void UpdateMonitoring()
	{
		SystemSnapshot snapshot = systemMonitor.GetSnapshot();

		overviewView.UpdateTelemetry(snapshot);
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

		overviewView.UpdateQualification(coordinator.WorkloadStatus, coordinator.LastReport);
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
			Shown -= OnShown;
			navigationService.ContextItemChanged -= OnContextItemChanged;
			if (overviewView is not null)
				overviewView.QualificationRequested -= OpenQualification;
			if (platformView is not null)
				platformView.DeviceSelected -= OnPlatformDeviceSelected;

			foreach (ButtonCommandBinding binding in commandBindings)
				binding.Dispose();

			timer.Dispose();
			shutdownCancellation.Dispose();
			applicationIcon?.Dispose();
		}

		base.Dispose(disposing);
	}
}
