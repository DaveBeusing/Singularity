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
	public IReadOnlyList<string> SelectedGpuIdentifiers { get; init; } =
		Array.Empty<string>();

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

	public IReadOnlyList<string> ResolveSelectedGpuIdentifiers()
	{
		IEnumerable<string> source = SelectedGpuIdentifiers.Count > 0
			? SelectedGpuIdentifiers
			: string.IsNullOrWhiteSpace(SelectedGpuIdentifier)
				? Array.Empty<string>()
				: [SelectedGpuIdentifier];

		return Array.AsReadOnly(
			source
				.Where(identifier => !string.IsNullOrWhiteSpace(identifier))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray());
	}

	public WorkloadOptions ToWorkloadOptions()
	{
		IReadOnlyList<string> selectedGpuIdentifiers =
			ResolveSelectedGpuIdentifiers();

		return new WorkloadOptions
		{
			EnableCpuWorkload = EnableCpuWorkload,
			CpuThreads = CpuThreads,
			EnableMemoryWorkload = EnableMemoryWorkload,
			MemoryGb = MemoryGb,
			EnableGpuWorkload = EnableGpuWorkload,
			GpuLoadPercent = GpuLoadPercent,
			SelectedGpuIdentifier = selectedGpuIdentifiers.Count == 1
				? selectedGpuIdentifiers[0]
				: null,
			SelectedGpuIdentifiers = selectedGpuIdentifiers
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
	IReadOnlyList<QualificationProfile> AvailableProfiles,
	IReadOnlyList<QualificationGpuOption> AvailableGpus,
	string SelectedGpu,
	QualificationFeedback? Feedback);

public sealed class QualificationWorkspaceState
{
	private QualificationFeedback? explicitFeedback;
	private bool gpuSelectionReviewRequired;
	private IReadOnlyList<QualificationGpuOption> availableGpus =
		Array.Empty<QualificationGpuOption>();
	private IReadOnlyList<QualificationProfile> availableProfiles =
		QualificationProfiles.All.Select(profile => profile.Snapshot()).ToArray();

	public QualificationConfiguration Configuration { get; private set; } =
		QualificationConfiguration.Default;

	public QualificationMode Mode { get; private set; } =
		QualificationMode.None;

	public IReadOnlyList<QualificationGpuOption> AvailableGpus => availableGpus;
	public IReadOnlyList<QualificationProfile> AvailableProfiles => availableProfiles;

	public void SetAvailableProfiles(IReadOnlyList<QualificationProfile> profiles)
	{
		ArgumentNullException.ThrowIfNull(profiles);

		QualificationProfile[] validProfiles = profiles
			.Where(profile => QualificationProfileValidator.Validate(profile).IsValid)
			.GroupBy(profile => profile.Id, StringComparer.Ordinal)
			.Select(group => group.First().Snapshot())
			.ToArray();

		foreach (QualificationProfile builtIn in QualificationProfiles.All)
		{
			if (!validProfiles.Any(profile =>
				string.Equals(profile.Id, builtIn.Id, StringComparison.Ordinal)))
			{
				validProfiles = validProfiles.Append(builtIn.Snapshot()).ToArray();
			}
		}

		availableProfiles = Array.AsReadOnly(validProfiles);
		QualificationProfile? selected = availableProfiles.FirstOrDefault(
			profile => string.Equals(
				profile.Id,
				Configuration.Profile.Id,
				StringComparison.Ordinal));

		if (selected is not null)
		{
			Configuration = Configuration with { Profile = selected.Snapshot() };
		}
		else
		{
			Configuration = Configuration with { Profile = QualificationProfiles.Standard.Snapshot() };
			explicitFeedback = new QualificationFeedback(
				QualificationFeedbackLevel.Warning,
				"The previously selected qualification profile is no longer available. Standard was selected.");
		}
	}

	public void SetAvailableGpus(IReadOnlyList<GpuInventory> gpus)
	{
		ArgumentNullException.ThrowIfNull(gpus);

		IReadOnlyList<QualificationGpuOption> options =
			QualificationGpuSelection.CreateOptions(gpus);
		IReadOnlyList<string> previousIdentifiers =
			Configuration.ResolveSelectedGpuIdentifiers();
		IReadOnlyList<QualificationGpuOption> selections =
			QualificationGpuSelection.ResolveSelections(
				options,
				previousIdentifiers);
		string[] selectedIdentifiers = selections
			.Select(option => option.Identifier)
			.ToArray();

		availableGpus = options;
		Configuration = Configuration with
		{
			SelectedGpuIdentifiers = Array.AsReadOnly(selectedIdentifiers),
			SelectedGpuIdentifier = selectedIdentifiers.Length == 1
				? selectedIdentifiers[0]
				: null,
			SelectedGpuName = CreateSelectedGpuLabel(selections)
		};

		bool selectionLost =
			previousIdentifiers.Count > 0 &&
			selectedIdentifiers.Length != previousIdentifiers.Count;
		if (selectionLost && Configuration.EnableGpuWorkload)
		{
			gpuSelectionReviewRequired = true;
			explicitFeedback = new QualificationFeedback(
				QualificationFeedbackLevel.Warning,
				"One or more previously selected GPUs are no longer available. Review the GPU selection before starting qualification.");
		}
	}

	public void SetConfiguration(QualificationConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		bool legacySelectionOverridesCollection =
			!string.IsNullOrWhiteSpace(configuration.SelectedGpuIdentifier) &&
			(configuration.SelectedGpuIdentifiers.Count != 1 ||
			 !string.Equals(
				 configuration.SelectedGpuIdentifiers[0],
				 configuration.SelectedGpuIdentifier,
				 StringComparison.OrdinalIgnoreCase));
		IReadOnlyList<string> selectedIdentifiers = legacySelectionOverridesCollection
			? [configuration.SelectedGpuIdentifier!]
			: configuration.ResolveSelectedGpuIdentifiers();

		if (selectedIdentifiers.Count == 0 &&
			string.IsNullOrWhiteSpace(configuration.SelectedGpuIdentifier) &&
			configuration.SelectedGpuIdentifiers.Count == 0)
		{
			Configuration = configuration;
		}
		else
		{
			Configuration = configuration with
			{
				SelectedGpuIdentifiers = selectedIdentifiers,
				SelectedGpuIdentifier = selectedIdentifiers.Count == 1
					? selectedIdentifiers[0]
					: null
			};
		}
		gpuSelectionReviewRequired = false;
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
		QualificationProfileValidationResult profileValidation =
			QualificationProfileValidator.Validate(Configuration.Profile);
		if (!profileValidation.IsValid)
			return $"Qualification profile is invalid: {string.Join(" ", profileValidation.Errors)}";

		if (!availableProfiles.Any(profile =>
			string.Equals(profile.Id, Configuration.Profile.Id, StringComparison.Ordinal)))
		{
			return "The selected qualification profile is no longer available.";
		}

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

		if (Configuration.EnableGpuWorkload && gpuSelectionReviewRequired)
			return "The GPU selection changed because a previously selected device is no longer available. Review and confirm the remaining GPU selection before starting qualification.";

		IReadOnlyList<string> selectedIdentifiers =
			Configuration.ResolveSelectedGpuIdentifiers();
		if (Configuration.EnableGpuWorkload && selectedIdentifiers.Count == 0)
			return "Select at least one GPU before starting qualification.";

		if (Configuration.EnableGpuWorkload)
		{
			IReadOnlyList<QualificationGpuOption> resolved =
				QualificationGpuSelection.ResolveSelections(
					availableGpus,
					selectedIdentifiers);
			if (resolved.Count != selectedIdentifiers.Count)
			{
				return "One or more selected GPUs are no longer available. Refresh the platform inventory and review the GPU selection.";
			}
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
		IReadOnlyList<string> selectedIdentifiers =
			Configuration.ResolveSelectedGpuIdentifiers();
		GpuTelemetrySnapshot?[] selectedTelemetry = selectedIdentifiers
			.Select(telemetry.FindGpuTelemetry)
			.ToArray();
		int unavailableGpuCount = selectedTelemetry.Count(
			gpu => gpu is null || !gpu.IsAvailable);

		bool requiredTelemetryUnavailable =
			(Configuration.EnableMemoryWorkload && telemetry.TotalPhysicalMemoryMb <= 0) ||
			(Configuration.EnableGpuWorkload &&
				(selectedIdentifiers.Count == 0 || unavailableGpuCount > 0));

		QualificationFeedback? feedback = ResolveFeedback(
			workload,
			progress,
			telemetry,
			selectedIdentifiers.Count,
			unavailableGpuCount,
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
			session.State == QualificationSessionState.Idle
				? Configuration.Profile.Name
				: session.Profile.Name,
			session.StartTime,
			session.Duration,
			BuildOverallState(workload.State, progress.State, session.State),
			CanStart(workload, progress, session),
			CanStart(workload, progress, session),
			CanStop(workload, progress, session),
			BuildCpuTelemetry(telemetry),
			BuildMemoryTelemetry(telemetry),
			BuildGpuTelemetry(telemetry, selectedIdentifiers, selectedTelemetry),
			requiredTelemetryUnavailable,
			availableProfiles,
			availableGpus,
			BuildSelectedGpuDisplay(selectedIdentifiers),
			feedback);
	}

	private bool CanStart(
		WorkloadStatus workload,
		QualificationProgress progress,
		QualificationSession session)
	{
		IReadOnlyList<string> selectedIdentifiers =
			Configuration.ResolveSelectedGpuIdentifiers();
		bool profileValid =
			QualificationProfileValidator.Validate(Configuration.Profile).IsValid &&
			availableProfiles.Any(profile =>
				string.Equals(profile.Id, Configuration.Profile.Id, StringComparison.Ordinal));
		bool gpuSelectionValid =
			!Configuration.EnableGpuWorkload ||
			(selectedIdentifiers.Count > 0 &&
			 QualificationGpuSelection.ResolveSelections(
				 availableGpus,
				 selectedIdentifiers).Count == selectedIdentifiers.Count);

		return Configuration.HasSelectedWorkload &&
			profileValid &&
			gpuSelectionValid &&
			!gpuSelectionReviewRequired &&
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
		int selectedGpuCount,
		int unavailableGpuCount,
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
			if (Configuration.EnableMemoryWorkload &&
				telemetry.TotalPhysicalMemoryMb <= 0)
			{
				unavailable.Add("system memory telemetry");
			}

			if (Configuration.EnableGpuWorkload &&
				(selectedGpuCount == 0 || unavailableGpuCount > 0))
			{
				unavailable.Add(
					selectedGpuCount <= 1
						? "selected GPU telemetry"
						: $"selected GPU telemetry ({unavailableGpuCount} of {selectedGpuCount} unavailable)");
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

	private static string BuildGpuTelemetry(
		SystemSnapshot telemetry,
		IReadOnlyList<string> selectedIdentifiers,
		IReadOnlyList<GpuTelemetrySnapshot?> selectedTelemetry)
	{
		if (selectedIdentifiers.Count == 0)
		{
			if (!telemetry.GpuTelemetryAvailable)
				return telemetry.GpuTelemetryStatus;

			string legacyPower = telemetry.GpuPowerAvailable
				? $" | {telemetry.GpuPowerWatts:0} W"
				: string.Empty;
			return $"{telemetry.GpuLoadPercent:0.0} % | {telemetry.GpuTemperatureCelsius} °C{legacyPower}";
		}

		if (selectedIdentifiers.Count == 1)
		{
			GpuTelemetrySnapshot? gpu = selectedTelemetry[0];
			if (gpu is null)
				return "Selected GPU telemetry unavailable";
			if (!gpu.IsAvailable)
				return gpu.Status;

			string power = gpu.PowerAvailable
				? $" | {gpu.PowerWatts:0} W"
				: string.Empty;
			return $"{gpu.LoadPercent:0.0} % | {gpu.TemperatureCelsius} °C{power}";
		}

		GpuTelemetrySnapshot[] available = selectedTelemetry
			.Where(gpu => gpu?.IsAvailable == true)
			.Cast<GpuTelemetrySnapshot>()
			.ToArray();
		if (available.Length == 0)
			return $"{selectedIdentifiers.Count} GPUs | telemetry unavailable";

		double averageLoad = available.Average(gpu => gpu.LoadPercent);
		int maximumTemperature = available.Max(gpu => gpu.TemperatureCelsius);
		return $"{selectedIdentifiers.Count} GPUs | {available.Length}/{selectedIdentifiers.Count} available | {averageLoad:0.0} % avg | {maximumTemperature} °C max";
	}

	private string BuildSelectedGpuDisplay(IReadOnlyList<string> selectedIdentifiers)
	{
		if (selectedIdentifiers.Count == 0)
			return "Not selected";

		List<string> names = [];
		foreach (string identifier in selectedIdentifiers)
		{
			QualificationGpuOption? option = availableGpus.FirstOrDefault(
				candidate => string.Equals(
					candidate.Identifier,
					identifier,
					StringComparison.OrdinalIgnoreCase));
			names.Add(option?.DisplayName ?? identifier);
		}

		return string.Join(", ", names);
	}

	private static string? CreateSelectedGpuLabel(
		IReadOnlyList<QualificationGpuOption> selections)
	{
		return selections.Count switch
		{
			0 => null,
			1 => selections[0].DisplayName,
			_ => $"{selections.Count} GPUs selected"
		};
	}
}
