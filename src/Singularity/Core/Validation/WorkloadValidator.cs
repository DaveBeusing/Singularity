// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Workloads;
using Singularity.Monitoring;

namespace Singularity.Core.Validation;

public sealed class WorkloadValidator
{
	private TimeSpan? gpuLoadStableSince;
	private TimeSpan? gpuRunningSince;

	public void Reset()
	{
		gpuLoadStableSince = null;
		gpuRunningSince = null;
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

		if (workload.GpuEnabled)
		{
			if (workload.State != WorkloadState.Running)
			{
				gpuLoadStableSince = null;
				gpuStatus = ValidationStatus.Warning;
				gpuMessage = "GPU initializing";
			}
			else if (sessionDuration - (gpuRunningSince ??= sessionDuration) < profile.GpuWarmupDuration)
			{
				gpuLoadStableSince = null;
				gpuStatus = ValidationStatus.Warning;
				gpuMessage = "GPU warming up";
			}
			else if (!telemetry.GpuTelemetryAvailable)
			{
				gpuLoadStableSince = null;
				gpuStatus = ValidationStatus.Warning;
				gpuMessage = telemetry.GpuTelemetryStatus;
			}
			else if (telemetry.GpuTemperatureCelsius > profile.GpuMaximumTemperatureCelsius)
			{
				gpuLoadStableSince = null;
				gpuStatus = ValidationStatus.Fail;
				gpuMessage = $"GPU temperature {telemetry.GpuTemperatureCelsius} °C";
			}
			else if (telemetry.GpuLoadPercent >= profile.GpuMinimumLoadPercent)
			{
				gpuLoadStableSince ??= sessionDuration;
				if (sessionDuration - gpuLoadStableSince.Value >= profile.GpuStabilityDuration)
				{
					gpuStatus = ValidationStatus.Pass;
					gpuMessage = $"GPU load {telemetry.GpuLoadPercent:0}%";
				}
				else
				{
					gpuStatus = ValidationStatus.Warning;
					gpuMessage = "GPU load stabilizing";
				}
			}
			else
			{
				gpuLoadStableSince = null;
				gpuStatus = ValidationStatus.Fail;
				gpuMessage = $"GPU load only {telemetry.GpuLoadPercent:0}%";
			}
		}
		else
		{
			gpuLoadStableSince = null;
			gpuRunningSince = null;
		}

		return new ValidationResult
		{
			CpuStatus = cpuStatus,
			MemoryStatus = memoryStatus,
			GpuStatus = gpuStatus,
			CpuMessage = cpuMessage,
			MemoryMessage = memoryMessage,
			GpuMessage = gpuMessage
		};
	}

}
