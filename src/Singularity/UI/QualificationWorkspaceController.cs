// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Application.Commands;
using Singularity.Core.Qualification;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Monitoring.Models;
using Singularity.UI.Navigation;
using Singularity.UI.Shell;
using Singularity.UI.Views;

namespace Singularity.UI;

public sealed class QualificationWorkspaceController : IDisposable
{
	private readonly QualificationCoordinator coordinator;
	private readonly QualificationWorkspaceState state;
	private readonly QualificationView view;
	private readonly QualificationToolPanelView toolPanelView;
	private readonly QualificationInspectorView inspectorView;
	private readonly NavigationService navigationService;
	private readonly CommandRouter commandRouter;
	private readonly ApplicationShell shell;
	private readonly ButtonCommandBinding startBinding;
	private readonly ButtonCommandBinding automatedBinding;
	private readonly ButtonCommandBinding stopBinding;
	private SystemSnapshot latestTelemetry = new();
	private WorkloadState previousWorkloadState;
	private QualificationRunState previousAutomatedState;
	private QualificationSessionState previousSessionState;
	private bool disposed;

	public QualificationWorkspaceController(
		QualificationCoordinator coordinator,
		QualificationWorkspaceState state,
		QualificationView view,
		QualificationToolPanelView toolPanelView,
		QualificationInspectorView inspectorView,
		NavigationService navigationService,
		CommandRouter commandRouter,
		ApplicationShell shell)
	{
		this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
		this.state = state ?? throw new ArgumentNullException(nameof(state));
		this.view = view ?? throw new ArgumentNullException(nameof(view));
		this.toolPanelView = toolPanelView ?? throw new ArgumentNullException(nameof(toolPanelView));
		this.inspectorView = inspectorView ?? throw new ArgumentNullException(nameof(inspectorView));
		this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
		this.commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
		this.shell = shell ?? throw new ArgumentNullException(nameof(shell));

		previousWorkloadState = coordinator.WorkloadStatus.State;
		previousAutomatedState = coordinator.Progress.State;
		previousSessionState = coordinator.Session.State;

		RegisterCommands();

		startBinding = new ButtonCommandBinding(view.StartButton, commandRouter, CommandId.StartQualification);
		automatedBinding = new ButtonCommandBinding(view.AutoButton, commandRouter, CommandId.AutomatedQualification);
		stopBinding = new ButtonCommandBinding(view.StopButton, commandRouter, CommandId.StopQualification);

		view.ConfigurationChanged += OnConfigurationChanged;
		view.ResultsRequested += OpenResults;
		navigationService.WorkspaceChanged += OnWorkspaceChanged;
		navigationService.ContextItemChanged += OnContextItemChanged;

		Render();
	}

	public QualificationWorkspaceSnapshot CurrentSnapshot =>
		state.CreateSnapshot(coordinator, latestTelemetry);

	public void Update(SystemSnapshot telemetry)
	{
		ArgumentNullException.ThrowIfNull(telemetry);

		latestTelemetry = telemetry;
		coordinator.Update(telemetry);
		CaptureStateTransitions();
		Render();
	}

	public void Refresh()
	{
		Render();
	}

	private void RegisterCommands()
	{
		commandRouter.Register(
			CommandId.StartQualification,
			StartManual,
			() => CurrentSnapshot.CanStartManual);

		commandRouter.Register(
			CommandId.AutomatedQualification,
			StartAutomated,
			() => CurrentSnapshot.CanStartAutomated);

		commandRouter.Register(
			CommandId.StopQualification,
			Stop,
			() => CurrentSnapshot.CanStop);
	}

	private void StartManual()
	{
		if (!ValidateConfiguration())
			return;

		bool started = coordinator.StartManual(
			state.Configuration.ToWorkloadOptions(),
			state.Configuration.Profile);

		if (!started)
		{
			state.SetFeedback(
				QualificationFeedbackLevel.Warning,
				"Qualification could not start because another run is active.");
			Render();
			return;
		}

		state.MarkStarted(QualificationMode.Manual);
		navigationService.Navigate(WorkspaceId.Qualification);
		shell.SetToolPanelVisible(true);
		CaptureStateTransitions();
		Render();
	}

	private void StartAutomated()
	{
		if (!ValidateConfiguration())
			return;

		try
		{
			bool started = coordinator.StartAutomated(
				state.Configuration.ToWorkloadOptions(),
				state.Configuration.Profile);

			if (!started)
			{
				state.SetFeedback(
					QualificationFeedbackLevel.Warning,
					"Automated qualification could not start because another run is active.");
				Render();
				return;
			}

			state.MarkStarted(QualificationMode.Automated);
			navigationService.Navigate(WorkspaceId.Qualification);
			shell.SetToolPanelVisible(true);
			CaptureStateTransitions();
			Render();
		}
		catch (InvalidOperationException ex)
		{
			state.SetFeedback(QualificationFeedbackLevel.Warning, ex.Message);
			Render();
		}
	}

	private void Stop()
	{
		QualificationRunState automatedState = coordinator.Progress.State;
		if (!coordinator.Stop())
		{
			state.SetFeedback(
				QualificationFeedbackLevel.Warning,
				"No active qualification can be stopped.");
			Render();
			return;
		}

		if (automatedState == QualificationRunState.Running)
		{
			state.SetFeedback(
				QualificationFeedbackLevel.Warning,
				"Automated qualification was cancelled.");
		}
		else
		{
			state.SetFeedback(
				QualificationFeedbackLevel.Information,
				"Qualification stopped and the current session was finalized.");
		}

		CaptureStateTransitions();
		Render();
	}

	private bool ValidateConfiguration()
	{
		string? error = state.ValidateConfiguration();
		if (error is null)
			return true;

		state.SetFeedback(QualificationFeedbackLevel.Warning, error);
		Render();
		return false;
	}

	private void OnConfigurationChanged(QualificationConfiguration configuration)
	{
		state.SetConfiguration(configuration);
		Render();
	}

	private void OpenResults()
	{
		navigationService.Navigate(WorkspaceId.Results);
	}

	private void OnWorkspaceChanged(WorkspaceDefinition workspace)
	{
		if (workspace.Id == WorkspaceId.Qualification)
			Render();
	}

	private void OnContextItemChanged(NavigationItem? item)
	{
		if (navigationService.ActiveWorkspace != WorkspaceId.Qualification)
			return;

		inspectorView.UpdateState(item?.Id, CurrentSnapshot);
	}

	private void CaptureStateTransitions()
	{
		WorkloadStatus workload = coordinator.WorkloadStatus;
		QualificationRunState automatedState = coordinator.Progress.State;
		QualificationSessionState sessionState = coordinator.Session.State;

		if (previousWorkloadState != WorkloadState.Failed &&
			workload.State == WorkloadState.Failed)
		{
			state.SetFeedback(
				QualificationFeedbackLevel.Failure,
				string.IsNullOrWhiteSpace(workload.Message)
					? "Qualification workload failed."
					: workload.Message);
		}

		if (previousAutomatedState != automatedState)
		{
			switch (automatedState)
			{
				case QualificationRunState.Completed:
					state.SetFeedback(
						QualificationFeedbackLevel.Information,
						"Automated qualification completed.");
					break;
				case QualificationRunState.Failed:
					state.SetFeedback(
						QualificationFeedbackLevel.Failure,
						"Automated qualification failed.");
					break;
				case QualificationRunState.Cancelled:
					state.SetFeedback(
						QualificationFeedbackLevel.Warning,
						"Automated qualification was cancelled.");
					break;
			}
		}

		if (state.Mode == QualificationMode.Manual &&
			previousSessionState == QualificationSessionState.Running &&
			sessionState == QualificationSessionState.Completed)
		{
			state.SetFeedback(
				QualificationFeedbackLevel.Information,
				"Manual qualification completed.");
		}

		previousWorkloadState = workload.State;
		previousAutomatedState = automatedState;
		previousSessionState = sessionState;
	}

	private void Render()
	{
		QualificationWorkspaceSnapshot snapshot = CurrentSnapshot;
		view.UpdateState(snapshot);
		toolPanelView.UpdateState(snapshot);
		inspectorView.UpdateState(
			navigationService.ActiveWorkspace == WorkspaceId.Qualification
				? navigationService.ActiveContextItem?.Id
				: null,
			snapshot);
		commandRouter.RefreshStates();
	}

	public void Dispose()
	{
		if (disposed)
			return;

		disposed = true;
		view.ConfigurationChanged -= OnConfigurationChanged;
		view.ResultsRequested -= OpenResults;
		navigationService.WorkspaceChanged -= OnWorkspaceChanged;
		navigationService.ContextItemChanged -= OnContextItemChanged;
		startBinding.Dispose();
		automatedBinding.Dispose();
		stopBinding.Dispose();
	}
}
