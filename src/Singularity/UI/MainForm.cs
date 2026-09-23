// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Application.Commands;
using Singularity.Application.Persistence;
using Singularity.Core.Reporting;
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
	private readonly QualificationWorkspaceState qualificationWorkspaceState;
	private readonly QualificationProfileCatalog profileCatalog;
	private readonly ReportsWorkspaceState reportsWorkspaceState = new();
	private readonly NavigationService navigationService = new(WorkspaceCatalog.CreateDefault());
	private readonly CommandRouter commandRouter = new();
	private readonly List<ButtonCommandBinding> commandBindings = [];
	private readonly System.Windows.Forms.Timer timer = new();
	private readonly CancellationTokenSource shutdownCancellation = new();
	private Icon? applicationIcon;
	private bool archiveFlushInProgress;
	private bool closeAfterArchiveFlush;

	private ApplicationShell shell = null!;
	private OverviewView overviewView = null!;
	private PlatformView platformView = null!;
	private PlatformInspectorView platformInspectorView = null!;
	private QualificationView qualificationView = null!;
	private QualificationToolPanelView qualificationToolPanelView = null!;
	private QualificationInspectorView qualificationInspectorView = null!;
	private QualificationWorkspaceController qualificationWorkspaceController = null!;
	private ResultsView resultsView = null!;
	private ResultsInspectorView resultsInspectorView = null!;
	private ReportsView reportsView = null!;
	private ReportsInspectorView reportsInspectorView = null!;
	private SettingsView settingsView = null!;

	public MainForm(
		QualificationCoordinator coordinator,
		ReportExportService reportExportService,
		SystemMonitor systemMonitor,
		PlatformInventoryState platformInventoryState,
		QualificationWorkspaceState qualificationWorkspaceState,
		QualificationProfileCatalog profileCatalog)
	{
		this.coordinator = coordinator;
		this.reportExportService = reportExportService;
		this.systemMonitor = systemMonitor;
		this.platformInventoryState = platformInventoryState;
		this.qualificationWorkspaceState = qualificationWorkspaceState;
		this.profileCatalog = profileCatalog;

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
		FormClosing += OnFormClosing;

		timer.Interval = 500;
		timer.Tick += (_, _) => UpdateMonitoring();
		timer.Start();
	}

	private ReportsWorkspaceSnapshot CurrentReportsSnapshot =>
		reportsWorkspaceState.CreateSnapshot(
			coordinator.EvidenceRecords,
			platformInventoryState.Current is not null);

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
			CommandId.ExportJson,
			ExportJsonReport,
			() => CurrentReportsSnapshot.CanExport);

		commandRouter.Register(
			CommandId.ExportHtml,
			ExportHtmlReport,
			() => CurrentReportsSnapshot.CanExport);

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
			qualificationToolPanelView = new QualificationToolPanelView();
			qualificationInspectorView = new QualificationInspectorView();
			resultsView = new ResultsView();
			resultsInspectorView = new ResultsInspectorView();
			reportsView = new ReportsView();
			reportsInspectorView = new ReportsInspectorView();
			settingsView = new SettingsView();

			shell.RegisterWorkspace(WorkspaceId.Overview, overviewView);
			shell.RegisterWorkspace(WorkspaceId.Platform, platformView);
			shell.RegisterInspectorContent(WorkspaceId.Platform, platformInspectorView);
			shell.RegisterWorkspace(WorkspaceId.Qualification, qualificationView);
			shell.RegisterInspectorContent(WorkspaceId.Qualification, qualificationInspectorView);
			shell.RegisterToolPanelContent(WorkspaceId.Qualification, qualificationToolPanelView);
			shell.RegisterWorkspace(WorkspaceId.Results, resultsView);
			shell.RegisterInspectorContent(WorkspaceId.Results, resultsInspectorView);
			shell.RegisterWorkspace(WorkspaceId.Reports, reportsView);
			shell.RegisterInspectorContent(WorkspaceId.Reports, reportsInspectorView);
			shell.RegisterWorkspace(WorkspaceId.Settings, settingsView);

			Controls.Add(shell);

			qualificationWorkspaceController = new QualificationWorkspaceController(
				coordinator,
				qualificationWorkspaceState,
				qualificationView,
				qualificationToolPanelView,
				qualificationInspectorView,
				navigationService,
				commandRouter,
				shell);

			overviewView.QualificationRequested += OpenQualification;
			platformView.DeviceSelected += OnPlatformDeviceSelected;
			reportsView.HistorySelectionRequested += OnHistorySelectionRequested;
			settingsView.SidebarVisibilityChanged += shell.SetSidebarVisible;
			settingsView.InspectorVisibilityChanged += shell.SetInspectorVisible;
			settingsView.ToolPanelVisibilityChanged += shell.SetToolPanelVisible;
			settingsView.ResetLayoutRequested += shell.ResetLayout;
			settingsView.ClearQualificationArchiveRequested += ClearQualificationArchive;
			settingsView.CreateQualificationProfileRequested += CreateQualificationProfile;
			settingsView.DuplicateQualificationProfileRequested += DuplicateQualificationProfile;
			settingsView.EditQualificationProfileRequested += EditQualificationProfile;
			settingsView.DeleteQualificationProfileRequested += DeleteQualificationProfile;
			settingsView.ResetQualificationProfilesRequested += ResetQualificationProfiles;
			shell.LayoutStateChanged += settingsView.UpdateState;
			navigationService.ContextItemChanged += OnContextItemChanged;

			settingsView.UpdateState(shell.LayoutState);
			RefreshQualificationProfiles();
			UpdateArchiveStatus();
			BindCommandButtons();
			RenderInventoryState();
			RenderQualificationState();
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
		commandBindings.Add(new ButtonCommandBinding(reportsView.ExportJsonButton, commandRouter, CommandId.ExportJson));
		commandBindings.Add(new ButtonCommandBinding(reportsView.ExportHtmlButton, commandRouter, CommandId.ExportHtml));
		commandBindings.Add(new ButtonCommandBinding(platformView.RefreshButton, commandRouter, CommandId.RefreshInventory));
	}

	private async void OnShown(object? sender, EventArgs e)
	{
		Shown -= OnShown;

		try
		{
			Task archiveLoad = coordinator.LoadArchiveAsync(shutdownCancellation.Token);
			Task profileLoad = profileCatalog.LoadAsync(shutdownCancellation.Token);
			Task inventoryLoad = RefreshPlatformInventoryAsync();
			await Task.WhenAll(archiveLoad, profileLoad, inventoryLoad);
		}
		catch (OperationCanceledException) when (shutdownCancellation.IsCancellationRequested)
		{
			return;
		}
		finally
		{
			if (!IsDisposed)
			{
				RefreshQualificationProfiles();
				RenderQualificationState();
				UpdateArchiveStatus();
			}
		}
	}

	private void OpenQualification()
	{
		navigationService.Navigate(WorkspaceId.Qualification);
	}

	private void OnContextItemChanged(NavigationItem? item)
	{
		switch (navigationService.ActiveWorkspace)
		{
			case WorkspaceId.Platform:
				platformView.SetCategory(item?.Id ?? "system");
				platformInspectorView.ShowSelection(null);
				break;

			case WorkspaceId.Results:
				resultsInspectorView.UpdateState(
					item?.Id,
					ResultsWorkspaceState.Create(coordinator.EvidenceRecords));
				break;

			case WorkspaceId.Reports:
				reportsView.FocusContext(item?.Id);
				reportsInspectorView.UpdateState(CurrentReportsSnapshot);
				break;
		}
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

	private void OnHistorySelectionRequested(int index)
	{
		ReportsWorkspaceSnapshot snapshot = reportsWorkspaceState.Select(
			index,
			coordinator.EvidenceRecords,
			platformInventoryState.Current is not null);
		RenderReports(snapshot);
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
		qualificationWorkspaceState.SetAvailableGpus(
			platformInventoryState.Current?.Gpus ?? []);
		qualificationWorkspaceController?.Refresh();

		overviewView.UpdateInventory(platformInventoryState);
		overviewView.UpdateQualification(coordinator.WorkloadStatus, coordinator.LastReport);
		platformView.UpdateInventory(platformInventoryState);
		RenderReports(CurrentReportsSnapshot);

		if (navigationService.ActiveWorkspace == WorkspaceId.Platform)
		{
			navigationService.SetSelection(null);
			platformInspectorView.ShowSelection(null);
		}
	}

	private void ExportJsonReport()
	{
		QualificationReport? report = CurrentReportsSnapshot.SelectedReport;
		if (report is null || platformInventoryState.Current is not { } inventory)
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

		ExportReport(
			() => reportExportService.ExportJson(dialog.FileName, report, inventory),
			"JSON");
	}

	private void ExportHtmlReport()
	{
		QualificationReport? report = CurrentReportsSnapshot.SelectedReport;
		if (report is null || platformInventoryState.Current is not { } inventory)
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

		ExportReport(
			() => reportExportService.ExportHtml(dialog.FileName, report, inventory),
			"HTML");
	}

	private void ExportReport(Action export, string format)
	{
		try
		{
			export();
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			MessageBox.Show(
				this,
				$"{format} report export failed.\r\n\r\n{ex.Message}",
				"Report export failed",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
	}

	private void UpdateMonitoring()
	{
		SystemSnapshot snapshot = systemMonitor.GetSnapshot();

		overviewView.UpdateTelemetry(snapshot);
		qualificationWorkspaceController.Update(snapshot);
		RenderQualificationState();
	}

	private void RenderQualificationState()
	{
		ResultsWorkspaceSnapshot results = ResultsWorkspaceState.Create(coordinator.EvidenceRecords);
		resultsView.UpdateState(results);
		resultsInspectorView.UpdateState(
			navigationService.ActiveWorkspace == WorkspaceId.Results
				? navigationService.ActiveContextItem?.Id
				: null,
			results);

		RenderReports(CurrentReportsSnapshot);
		overviewView.UpdateQualification(coordinator.WorkloadStatus, coordinator.LastReport);
		UpdateArchiveStatus();
		UpdateWorkloadStatus();
	}

	private void RenderReports(ReportsWorkspaceSnapshot snapshot)
	{
		reportsView.UpdateState(snapshot);
		reportsInspectorView.UpdateState(snapshot);
		commandRouter.RefreshStates();
	}

	private void UpdateWorkloadStatus()
	{
		QualificationWorkspaceSnapshot snapshot = qualificationWorkspaceController.CurrentSnapshot;

		string statusText = snapshot.OverallState;
		StatusVisualState visualState = statusText switch
		{
			"FAILED" => StatusVisualState.Failure,
			"STARTING" or "STOPPING" or "CANCELLED" => StatusVisualState.Warning,
			"RUNNING" => StatusVisualState.Active,
			"COMPLETED" => StatusVisualState.Success,
			_ => StatusVisualState.Neutral
		};

		if (snapshot.SessionState is QualificationSessionState.Completed or QualificationSessionState.Failed &&
			coordinator.EvidenceRecords.Count > 0)
		{
			ValidationStatus result = coordinator.EvidenceRecords[0].Result;
			statusText = StatusStyle.Format(result);
			visualState = result switch
			{
				ValidationStatus.Pass => StatusVisualState.Success,
				ValidationStatus.Warning => StatusVisualState.Warning,
				ValidationStatus.Fail => StatusVisualState.Failure,
				_ => StatusVisualState.Neutral
			};
		}

		shell.SetGlobalStatus(statusText, visualState);
		shell.SetStatusDetails(
			$"{snapshot.SessionProfile} • CPU {CompactTelemetry(snapshot.CpuTelemetry)} • RAM {CompactTelemetry(snapshot.MemoryTelemetry)} • GPU {CompactTelemetry(snapshot.GpuTelemetry)}");
		commandRouter.RefreshStates();
	}

	private void RefreshQualificationProfiles()
	{
		if (settingsView is null)
			return;

		qualificationWorkspaceState.SetAvailableProfiles(profileCatalog.Profiles);
		qualificationWorkspaceController?.Refresh();
		settingsView.UpdateProfileState(
			profileCatalog.Profiles,
			profileCatalog.State,
			profileCatalog.LastError,
			profileCatalog.StoragePath);
	}

	private async void CreateQualificationProfile()
	{
		QualificationProfile template = QualificationProfiles.Standard with
		{
			Id = "custom.pending",
			Origin = QualificationProfileOrigin.Custom,
			Name = "Custom profile"
		};
		using QualificationProfileEditorDialog dialog = new(template, "Create qualification profile");
		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;
		await profileCatalog.CreateAsync(dialog.ResultProfile, shutdownCancellation.Token);
		RefreshQualificationProfiles();
	}

	private async void DuplicateQualificationProfile(QualificationProfile profile)
	{
		await profileCatalog.DuplicateAsync(profile, shutdownCancellation.Token);
		RefreshQualificationProfiles();
	}

	private async void EditQualificationProfile(QualificationProfile profile)
	{
		if (profile.IsBuiltIn)
			return;
		using QualificationProfileEditorDialog dialog = new(profile, "Edit qualification profile");
		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;
		await profileCatalog.UpdateAsync(dialog.ResultProfile, shutdownCancellation.Token);
		RefreshQualificationProfiles();
	}

	private async void DeleteQualificationProfile(QualificationProfile profile)
	{
		if (profile.IsBuiltIn)
			return;
		if (MessageBox.Show(this, $"Delete custom qualification profile '{profile.Name}'?", "Delete qualification profile", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
			return;
		await profileCatalog.DeleteAsync(profile.Id, shutdownCancellation.Token);
		RefreshQualificationProfiles();
	}

	private async void ResetQualificationProfiles()
	{
		if (MessageBox.Show(this, "Delete all custom qualification profiles? Built-in profiles remain unchanged.", "Reset qualification profiles", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
			return;
		await profileCatalog.ResetCustomAsync(shutdownCancellation.Token);
		RefreshQualificationProfiles();
	}

	private async void ClearQualificationArchive()
	{
		DialogResult confirmation = MessageBox.Show(
			this,
			"Delete all locally stored qualification evidence and clear the current Results/Reports history? This action cannot be undone.",
			"Clear qualification archive",
			MessageBoxButtons.YesNo,
			MessageBoxIcon.Warning,
			MessageBoxDefaultButton.Button2);

		if (confirmation != DialogResult.Yes)
			return;

		try
		{
			await coordinator.ClearArchiveAsync(shutdownCancellation.Token);
		}
		catch (OperationCanceledException) when (shutdownCancellation.IsCancellationRequested)
		{
			return;
		}

		if (IsDisposed)
			return;

		RenderQualificationState();
		UpdateArchiveStatus();

		if (coordinator.ArchiveState == QualificationArchiveState.Failed)
		{
			MessageBox.Show(
				this,
				$"The qualification archive could not be cleared.\r\n\r\n{coordinator.ArchiveError}",
				"Qualification archive",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
	}

	private void UpdateArchiveStatus()
	{
		if (settingsView is null)
			return;

		settingsView.UpdateArchiveState(
			coordinator.ArchiveState,
			coordinator.ArchivedRecordCount,
			coordinator.ArchiveError,
			coordinator.ArchivePath);
	}

	private async void OnFormClosing(object? sender, FormClosingEventArgs e)
	{
		if (closeAfterArchiveFlush)
			return;

		if (archiveFlushInProgress)
		{
			e.Cancel = true;
			return;
		}

		Task flushTask = coordinator.FlushArchiveAsync();
		if (flushTask.IsCompletedSuccessfully)
		{
			closeAfterArchiveFlush = true;
			return;
		}

		e.Cancel = true;
		archiveFlushInProgress = true;
		timer.Stop();

		try
		{
			await flushTask;
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				this,
				$"The latest qualification archive write did not complete.\r\n\r\n{ex.Message}",
				"Qualification archive",
				MessageBoxButtons.OK,
				MessageBoxIcon.Warning);
		}
		finally
		{
			archiveFlushInProgress = false;
			closeAfterArchiveFlush = true;
			if (!IsDisposed)
				BeginInvoke(Close);
		}
	}

	private static string CompactTelemetry(string value)
	{
		int separator = value.IndexOf(" | ", StringComparison.Ordinal);
		return separator < 0 ? value : value[..separator].Trim();
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
			FormClosing -= OnFormClosing;
			navigationService.ContextItemChanged -= OnContextItemChanged;
			if (overviewView is not null)
				overviewView.QualificationRequested -= OpenQualification;
			if (platformView is not null)
				platformView.DeviceSelected -= OnPlatformDeviceSelected;
			if (reportsView is not null)
				reportsView.HistorySelectionRequested -= OnHistorySelectionRequested;
			if (settingsView is not null && shell is not null)
			{
				settingsView.SidebarVisibilityChanged -= shell.SetSidebarVisible;
				settingsView.InspectorVisibilityChanged -= shell.SetInspectorVisible;
				settingsView.ToolPanelVisibilityChanged -= shell.SetToolPanelVisible;
				settingsView.ResetLayoutRequested -= shell.ResetLayout;
				settingsView.ClearQualificationArchiveRequested -= ClearQualificationArchive;
				settingsView.CreateQualificationProfileRequested -= CreateQualificationProfile;
				settingsView.DuplicateQualificationProfileRequested -= DuplicateQualificationProfile;
				settingsView.EditQualificationProfileRequested -= EditQualificationProfile;
				settingsView.DeleteQualificationProfileRequested -= DeleteQualificationProfile;
				settingsView.ResetQualificationProfilesRequested -= ResetQualificationProfiles;
				shell.LayoutStateChanged -= settingsView.UpdateState;
			}

			qualificationWorkspaceController?.Dispose();

			foreach (ButtonCommandBinding binding in commandBindings)
				binding.Dispose();

			timer.Dispose();
			shutdownCancellation.Dispose();
			applicationIcon?.Dispose();
		}

		base.Dispose(disposing);
	}
}
