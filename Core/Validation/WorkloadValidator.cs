// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Workloads;
using Singularity.Monitoring;

namespace Singularity.Core.Validation;

public sealed class WorkloadValidator
{
	public ValidationResult Validate(
		WorkloadStatus workload,
		SystemSnapshot telemetry)
	{
		ValidationStatus cpuStatus = ValidationStatus.Unknown;
		string cpuMessage = "CPU workload disabled";

		if (workload.CpuEnabled)
		{
			if (telemetry.CpuLoadPercent >= 80)
			{
				cpuStatus = ValidationStatus.Pass;
				cpuMessage = $"CPU load {telemetry.CpuLoadPercent:0}%";
			}
			else if (telemetry.CpuLoadPercent >= 50)
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

			long passLimit = (long)(expectedMb * 0.90);

			long warningLimit = (long)(expectedMb * 0.75);

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

		return new ValidationResult
		{
			CpuStatus = cpuStatus,
			MemoryStatus = memoryStatus,
			CpuMessage = cpuMessage,
			MemoryMessage = memoryMessage
		};
	}

}