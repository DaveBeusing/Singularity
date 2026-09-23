// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Qualification;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Hardware.Models;
using Singularity.Monitoring.Models;

namespace Singularity.Application;

public enum QualificationMode
{
	None,
	Manual,
	Automated
}

public enum QualificationFeedbackLevel
{
	Information,
	Warning,
	Failure
}

public sealed record QualificationFeedback(
	QualificationFeedbackLevel Level,
	string Message);

public sealed record QualificationConfiguration(
	bool EnableCpuWorkload,
	int CpuThreads,
	bool EnableMemoryWorkload,
	int MemoryGb,
	bool EnableGpuWorkload,
	int GpuLoadPercent,
	QualificationProfile Profile)
{
	public string? SelectedGpuIdentifier { get; init; }
	public string? SelectedGpuName { get; init; }

	public static QualificationConfiguration Default { get; } = new(
		true,
		Environment.ProcessorCount,
		true,
		8,
		false,
		100,
		QualificationProfiles.Standard);

	public bool HasSelectedWorkload =>
		EnableCpuWorkload ||
		EnableMemoryWorkload ||
		EnableGpuWorkload;

	public WorkloadOptions ToWorkloadOptions()
	{
		return new WorkloadOptions
		{
			EnableCpuWorkload = EnableCpuWorkload,
			CpuThreads = CpuThreads,
			EnableMemoryWorkload = EnableMemoryWorkload,
			MemoryGb = MemoryGb,
			EnableGpuWorkload = EnableGpuWorkload,
			GpuLoadPercent = GpuLoadPercent,
			SelectedGpuIdentifier = SelectedGpuIdentifier
		};
	}
}

public sealed record QualificationWorkspaceSnapshot(
	QualificationConfiguration Configuration,
	QualificationMode Mode,
	WorkloadState WorkloadState,
	string WorkloadMessage,
	QualificationRunState AutomatedState,
	string ActiveStep,
	int StepNumber,
	int StepCount,
	double ProgressPercent,
	QualificationSessionState SessionState,
	string SessionProfile,
	DateTime? StartedAt,
	TimeSpan Elapsed,
	string OverallState,
	bool CanStartManual,
	bool CanStartAutomated,
	bool CanStop,
	string CpuTelemetry,
	string MemoryTelemetry,
	string GpuTelemetry,
	bool RequiredTelemetryUnavailable,
	IReadOnlyList<QualificationGpuOption> AvailableGpus,
	string SelectedGpu,
	QualificationFeedback? Feedback);

public sealed class QualificationWorkspaceState
{
	private QualificationFeedback? explicitFeedback;
	private IReadOnlyList<QualificationGpuOption> availableGpus =
		Array.Empty<QualificationGpuOption>();

	public QualificationConfiguration Configuration { get; private set; } =
		QualificationConfiguration.Default;

	public QualificationMode Mode { get; private set; } =
		QualificationMode.None;

	public IReadOnlyList<QualificationGpuOption> AvailableGpus => availableGpus;

	public void SetAvailableGpus(IReadOnlyList<GpuInventory> gpus)
	{
		ArgumentNullException.ThrowIfNull(gpus);

		IReadOnlyList<QualificationGpuOption> options =
			QualificationGpuSelection.CreateOptions(gpus);
		string? previousIdentifier = Configuration.SelectedGpuIdentifier;
		QualificationGpuOption? selection =
			QualificationGpuSelection.ResolveSelection(options, previousIdentifier);

		availableGpus = options;
		Configuration = Configuration with
		{
			SelectedGpuIdentifier = selection?.Identifier,
			SelectedGpuName = selection?.DisplayName
		};

		if (!string.IsNullOrWhiteSpace(previousIdentifier) &&
			selection is null &&
			Configuration.EnableGpuWorkload)
		{
			explicitFeedback = new QualificationFeedback(
				QualificationFeedbackLevel.Warning,
				"The previously selected GPU is no longer available. Select a GPU before starting qualification.");
		}
	}

	public void SetConfiguration(QualificationConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		Configuration = configuration;
		explicitFeedback = null;
	}

	public void MarkStarted(QualificationMode mode)
	{
		if (mode == QualificationMode.None)
			throw new ArgumentOutOfRangeException(nameof(mode));

		Mode = mode;
		explicitFeedback = null;
	}

	public void SetFeedback(QualificationFeedbackLevel level, string message)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(message);
		explicitFeedback = new QualificationFeedback(level, message);
	}

	public void ClearFeedback()
	{
		explicitFeedback = null;
	}

	public string? ValidateConfiguration()
	{
		if (!Configuration.HasSelectedWorkload)
			return "Select at least one workload before starting qualification.";

		if (Configuration.EnableCpuWorkload && Configuration.CpuThreads <= 0)
			return "CPU thread count must be greater than zero.";

		if (Configuration.EnableMemoryWorkload && Configuration.MemoryGb <= 0)
			return "Memory allocation must be greater than zero.";

		if (Configuration.EnableGpuWorkload && Configuration.GpuLoadPercent is < 1 or > 100)
			return "GPU target load must be between 1 and 100 percent.";

		if (Configuration.EnableGpuWorkload && availableGpus.Count == 0)
			return "No GPU with a stable device identity is available for qualification.";

		if (Configuration.EnableGpuWorkload &&
			string.IsNullOrWhiteSpace(Configuration.SelectedGpuIdentifier))
		{
			return "Select a GPU before starting qualification.";
		}

		if (Configuration.EnableGpuWorkload &&
			QualificationGpuSelection.ResolveSelection(
				availableGpus,
				Configuration.SelectedGpuIdentifier) is null)
		{
			return "The selected GPU is no longer available. Refresh the platform inventory and select an available GPU.";
		}

		return null;
	}

	public QualificationWorkspaceSnapshot CreateSnapshot(
		QualificationCoordinator coordinator,
		SystemSnapshot telemetry)
	{
		ArgumentNullException.ThrowIfNull(coordinator);
		ArgumentNullException.ThrowIfNull(telemetry);

		WorkloadStatus workload = coordinator.WorkloadStatus;
		QualificationProgress progress = coordinator.Progress;
		QualificationSession session = coordinator.Session;
		GpuTelemetrySnapshot? selectedGpu = telemetry.FindGpuTelemetry(
			Configuration.SelectedGpuIdentifier);

		bool requiredTelemetryUnavailable =
			(Configuration.EnableMemoryWorkload && telemetry.TotalPhysicalMemoryMb <= 0) ||
			(Configuration.EnableGpuWorkload &&
				(selectedGpu is null || !selectedGpu.IsAvailable));

		QualificationFeedback? feedback = ResolveFeedback(
			workload,
			progress,
			telemetry,
			selectedGpu,
			requiredTelemetryUnavailable);

		return new QualificationWorkspaceSnapshot(
			Configuration,
			Mode,
			workload.State,
			workload.Message,
			progress.State,
			progress.StepName,
			progress.StepNumber,
			progress.StepCount,
			progress.Percent,
			session.State,
			session.State == QualificationSessionState.Idle ? Configuration.Profile.Name : session.Profile.Name,
			session.StartTime,
			session.Duration,
			BuildOverallState(workload.State, progress.State, session.State),
			CanStart(workload, progress, session),
			CanStart(workload, progress, session),
			CanStop(workload, progress, session),
			BuildCpuTelemetry(telemetry),
			BuildMemoryTelemetry(telemetry),
			BuildGpuTelemetry(telemetry, selectedGpu),
			requiredTelemetryUnavailable,
			availableGpus,
			Configuration.SelectedGpuName ?? Configuration.SelectedGpuIdentifier ?? "Not selected",
			feedback);
	}

	private bool CanStart(
		WorkloadStatus workload,
		QualificationProgress progress,
		QualificationSession session)
	{
		return Configuration.HasSelectedWorkload &&
			(!Configuration.EnableGpuWorkload ||
				(!string.IsNullOrWhiteSpace(Configuration.SelectedGpuIdentifier) &&
				 QualificationGpuSelection.ResolveSelection(
					 availableGpus,
					 Configuration.SelectedGpuIdentifier) is not null)) &&
			session.State != QualificationSessionState.Running &&
			progress.State != QualificationRunState.Running &&
			workload.State is WorkloadState.Stopped or WorkloadState.Failed;
	}

	private static bool CanStop(
		WorkloadStatus workload,
		QualificationProgress progress,
		QualificationSession session)
	{
		if (session.State != QualificationSessionState.Running)
			return false;

		return progress.State == QualificationRunState.Running ||
			workload.State is WorkloadState.Starting or WorkloadState.Running or WorkloadState.Failed;
	}

	private QualificationFeedback? ResolveFeedback(
		WorkloadStatus workload,
		QualificationProgress progress,
		SystemSnapshot telemetry,
		GpuTelemetrySnapshot? selectedGpu,
		bool requiredTelemetryUnavailable)
	{
		if (workload.State == WorkloadState.Failed)
		{
			return new QualificationFeedback(
				QualificationFeedbackLevel.Failure,
				string.IsNullOrWhiteSpace(workload.Message)
					? "Qualification workload failed."
					: workload.Message);
		}

		if (progress.State == QualificationRunState.Failed)
		{
			return new QualificationFeedback(
				QualificationFeedbackLevel.Failure,
				"Automated qualification failed.");
		}

		if (progress.State == QualificationRunState.Cancelled)
		{
			return new QualificationFeedback(
				QualificationFeedbackLevel.Warning,
				"Automated qualification was cancelled.");
		}

		if (explicitFeedback is not null)
			return explicitFeedback;

		if (requiredTelemetryUnavailable)
		{
			List<string> unavailable = [];
			if (Configuration.EnableMemoryWorkload && telemetry.TotalPhysicalMemoryMb <= 0)
				unavailable.Add("system memory telemetry");
			if (Configuration.EnableGpuWorkload &&
				(selectedGpu is null || !selectedGpu.IsAvailable))
			{
				unavailable.Add("selected GPU telemetry");
			}

			return new QualificationFeedback(
				QualificationFeedbackLevel.Warning,
				$"Required {string.Join(" and ", unavailable)} unavailable.");
		}

		return null;
	}

	private static string BuildOverallState(
		WorkloadState workloadState,
		QualificationRunState automatedState,
		QualificationSessionState sessionState)
	{
		if (workloadState != WorkloadState.Stopped)
			return workloadState.ToString().ToUpperInvariant();

		return automatedState switch
		{
			QualificationRunState.Running => "RUNNING",
			QualificationRunState.Completed => "COMPLETED",
			QualificationRunState.Cancelled => "CANCELLED",
			QualificationRunState.Failed => "FAILED",
			_ => sessionState switch
			{
				QualificationSessionState.Running => "RUNNING",
				QualificationSessionState.Completed => "COMPLETED",
				QualificationSessionState.Failed => "FAILED",
				_ => "READY"
			}
		};
	}

	private static string BuildCpuTelemetry(SystemSnapshot telemetry)
	{
		string temperature = telemetry.CpuTemperatureAvailable
			? $"{telemetry.CpuTemperatureCelsius:0} °C"
			: telemetry.CpuTemperatureStatus;

		return $"{telemetry.CpuLoadPercent:0.0} % | {temperature}";
	}

	private static string BuildMemoryTelemetry(SystemSnapshot telemetry)
	{
		if (telemetry.TotalPhysicalMemoryMb <= 0)
			return "Unavailable";

		return $"{telemetry.UsedPhysicalMemoryPercent:0.0} % | {telemetry.UsedPhysicalMemoryMb:N0} / {telemetry.TotalPhysicalMemoryMb:N0} MB";
	}

	private string BuildGpuTelemetry(
		SystemSnapshot telemetry,
		GpuTelemetrySnapshot? selectedGpu)
	{
		if (!string.IsNullOrWhiteSpace(Configuration.SelectedGpuIdentifier))
		{
			if (selectedGpu is null)
				return "Selected GPU telemetry unavailable";

			if (!selectedGpu.IsAvailable)
				return selectedGpu.Status;

			string selectedPower = selectedGpu.PowerAvailable
				? $" | {selectedGpu.PowerWatts:0} W"
				: string.Empty;
			return $"{selectedGpu.LoadPercent:0.0} % | {selectedGpu.TemperatureCelsius} °C{selectedPower}";
		}

		if (!telemetry.GpuTelemetryAvailable)
			return telemetry.GpuTelemetryStatus;

		string power = telemetry.GpuPowerAvailable
			? $" | {telemetry.GpuPowerWatts:0} W"
			: string.Empty;

		return $"{telemetry.GpuLoadPercent:0.0} % | {telemetry.GpuTemperatureCelsius} °C{power}";
	}
}
