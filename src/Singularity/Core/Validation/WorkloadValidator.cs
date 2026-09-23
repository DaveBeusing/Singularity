// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Workloads;
using Singularity.Monitoring.Models;

namespace Singularity.Core.Validation;

public sealed class WorkloadValidator
{
	private const string LegacyGpuKey = "__legacy__";

	private readonly Dictionary<string, TimeSpan> gpuLoadStableSince =
		new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, TimeSpan> gpuRunningSince =
		new(StringComparer.OrdinalIgnoreCase);

	public void Reset()
	{
		gpuLoadStableSince.Clear();
		gpuRunningSince.Clear();
	}

	public ValidationResult Validate(
		WorkloadStatus workload,
		SystemSnapshot telemetry,
		QualificationProfile profile,
		TimeSpan sessionDuration)
	{
		ValidationStatus cpuStatus = ValidationStatus.Unknown;
		string cpuMessage = "CPU workload disabled";

		if (workload.CpuEnabled)
		{
			if (telemetry.CpuLoadPercent >= profile.CpuMinimumLoadPercent)
			{
				cpuStatus = ValidationStatus.Pass;
				cpuMessage = $"CPU load {telemetry.CpuLoadPercent:0}%";
			}
			else if (telemetry.CpuLoadPercent >= profile.CpuWarningLoadPercent)
			{
				cpuStatus = ValidationStatus.Warning;
				cpuMessage = $"CPU load {telemetry.CpuLoadPercent:0}%";
			}
			else
			{
				cpuStatus = ValidationStatus.Fail;
				cpuMessage = $"CPU load only {telemetry.CpuLoadPercent:0}%";
			}
		}

		ValidationStatus memoryStatus = ValidationStatus.Unknown;
		string memoryMessage = "Memory workload disabled";

		if (workload.MemoryEnabled)
		{
			long expectedMb = workload.MemoryGb * 1024;
			long passLimit = (long)(expectedMb * profile.MemoryAllocationTolerancePercent / 100.0);
			long warningLimit = (long)(expectedMb * profile.MemoryWarningTolerancePercent / 100.0);

			if (workload.MemoryAllocatedMb >= passLimit)
			{
				memoryStatus = ValidationStatus.Pass;
				memoryMessage = $"{workload.MemoryAllocatedMb} MB allocated";
			}
			else if (workload.MemoryAllocatedMb >= warningLimit)
			{
				memoryStatus = ValidationStatus.Warning;
				memoryMessage = $"{workload.MemoryAllocatedMb} MB allocated";
			}
			else
			{
				memoryStatus = ValidationStatus.Fail;
				memoryMessage = $"{workload.MemoryAllocatedMb} MB allocated";
			}
		}

		ValidationStatus gpuStatus = ValidationStatus.Unknown;
		string gpuMessage = "GPU workload disabled";
		IReadOnlyList<GpuValidationResult> gpuDevices =
			Array.Empty<GpuValidationResult>();

		if (workload.GpuEnabled)
		{
			IReadOnlyList<string> selectedIdentifiers =
				workload.ResolveSelectedGpuIdentifiers();

			if (selectedIdentifiers.Count == 0)
			{
				GpuValidationResult legacy = ValidateGpu(
					LegacyGpuKey,
					string.Empty,
					telemetry.FindGpuTelemetry(null),
					explicitSelection: false,
					workload.State,
					telemetry,
					profile,
					sessionDuration);
				gpuStatus = legacy.Status;
				gpuMessage = legacy.Message;
			}
			else
			{
				GpuValidationResult[] results =
					new GpuValidationResult[selectedIdentifiers.Count];

				for (int index = 0; index < selectedIdentifiers.Count; index++)
				{
					string identifier = selectedIdentifiers[index];
					results[index] = ValidateGpu(
						identifier,
						identifier,
						telemetry.FindGpuTelemetry(identifier),
						explicitSelection: true,
						workload.State,
						telemetry,
						profile,
						sessionDuration);
				}

				gpuDevices = Array.AsReadOnly(results);
				gpuStatus = AggregateStatus(results);
				gpuMessage = BuildAggregateMessage(results, gpuStatus);
			}
		}
		else
		{
			gpuLoadStableSince.Clear();
			gpuRunningSince.Clear();
		}

		return new ValidationResult
		{
			CpuStatus = cpuStatus,
			MemoryStatus = memoryStatus,
			GpuStatus = gpuStatus,
			CpuMessage = cpuMessage,
			MemoryMessage = memoryMessage,
			GpuMessage = gpuMessage,
			GpuDevices = gpuDevices
		};
	}

	private GpuValidationResult ValidateGpu(
		string key,
		string identifier,
		GpuTelemetrySnapshot? selectedGpu,
		bool explicitSelection,
		WorkloadState workloadState,
		SystemSnapshot telemetry,
		QualificationProfile profile,
		TimeSpan sessionDuration)
	{
		string name = selectedGpu?.Name ?? string.Empty;
		if (string.IsNullOrWhiteSpace(name))
			name = string.IsNullOrWhiteSpace(identifier) ? "GPU" : identifier;

		ValidationStatus status;
		string message;
		bool telemetryAvailable = selectedGpu?.IsAvailable ??
			(!explicitSelection && telemetry.GpuTelemetryAvailable);

		if (workloadState != WorkloadState.Running)
		{
			gpuLoadStableSince.Remove(key);
			status = ValidationStatus.Warning;
			message = "GPU initializing";
		}
		else
		{
			if (!gpuRunningSince.TryGetValue(key, out TimeSpan runningSince))
			{
				runningSince = sessionDuration;
				gpuRunningSince[key] = runningSince;
			}

			if (sessionDuration - runningSince < profile.GpuWarmupDuration)
			{
				gpuLoadStableSince.Remove(key);
				status = ValidationStatus.Warning;
				message = "GPU warming up";
			}
			else if (explicitSelection && selectedGpu is null)
			{
				gpuLoadStableSince.Remove(key);
				status = ValidationStatus.Warning;
				message = "Selected GPU telemetry unavailable";
			}
			else if (selectedGpu is not null && !selectedGpu.IsAvailable)
			{
				gpuLoadStableSince.Remove(key);
				status = ValidationStatus.Warning;
				message = selectedGpu.Status;
			}
			else if (!explicitSelection && !telemetry.GpuTelemetryAvailable)
			{
				gpuLoadStableSince.Remove(key);
				status = ValidationStatus.Warning;
				message = telemetry.GpuTelemetryStatus;
			}
			else
			{
				double gpuLoad = selectedGpu?.LoadPercent ?? telemetry.GpuLoadPercent;
				int gpuTemperature = selectedGpu?.TemperatureCelsius ?? telemetry.GpuTemperatureCelsius;

				if (gpuTemperature > profile.GpuMaximumTemperatureCelsius)
				{
					gpuLoadStableSince.Remove(key);
					status = ValidationStatus.Fail;
					message = $"GPU temperature {gpuTemperature} °C";
				}
				else if (gpuLoad >= profile.GpuMinimumLoadPercent)
				{
					if (!gpuLoadStableSince.TryGetValue(key, out TimeSpan stableSince))
					{
						stableSince = sessionDuration;
						gpuLoadStableSince[key] = stableSince;
					}

					if (sessionDuration - stableSince >= profile.GpuStabilityDuration)
					{
						status = ValidationStatus.Pass;
						message = $"GPU load {gpuLoad:0}%";
					}
					else
					{
						status = ValidationStatus.Warning;
						message = "GPU load stabilizing";
					}
				}
				else
				{
					gpuLoadStableSince.Remove(key);
					status = ValidationStatus.Fail;
					message = $"GPU load only {gpuLoad:0}%";
				}
			}
		}

		return new GpuValidationResult(
			identifier,
			name,
			status,
			message,
			telemetryAvailable);
	}

	private static ValidationStatus AggregateStatus(
		IReadOnlyList<GpuValidationResult> results)
	{
		if (results.Any(result => result.Status == ValidationStatus.Fail))
			return ValidationStatus.Fail;
		if (results.Any(result => result.Status == ValidationStatus.Warning))
			return ValidationStatus.Warning;
		if (results.Any(result => result.Status == ValidationStatus.Pass))
			return ValidationStatus.Pass;
		return ValidationStatus.Unknown;
	}

	private static string BuildAggregateMessage(
		IReadOnlyList<GpuValidationResult> results,
		ValidationStatus aggregateStatus)
	{
		if (results.Count == 1)
			return results[0].Message;

		GpuValidationResult? firstRelevant = results.FirstOrDefault(
			result => result.Status == aggregateStatus);
		string detail = firstRelevant is null
			? string.Empty
			: $" · {firstRelevant.Name}: {firstRelevant.Message}";

		return $"{results.Count} GPUs {aggregateStatus.ToString().ToUpperInvariant()}{detail}";
	}
}
