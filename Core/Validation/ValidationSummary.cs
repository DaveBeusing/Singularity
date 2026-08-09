// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

public sealed class ValidationSummary
{
	public ValidationResult Result { get; }

	public ValidationSummary(
		ValidationResult result)
	{
		Result = result;
	}

	public ValidationStatus OverallStatus
	{
		get
		{
			if (Result.CpuStatus == ValidationStatus.Fail ||
				Result.MemoryStatus == ValidationStatus.Fail ||
				Result.GpuStatus == ValidationStatus.Fail)
			{
				return ValidationStatus.Fail;
			}

			if (Result.CpuStatus == ValidationStatus.Warning ||
				Result.MemoryStatus == ValidationStatus.Warning ||
				Result.GpuStatus == ValidationStatus.Warning)
			{
				return ValidationStatus.Warning;
			}

			if (Result.CpuStatus == ValidationStatus.Pass ||
				Result.MemoryStatus == ValidationStatus.Pass ||
				Result.GpuStatus == ValidationStatus.Pass)
			{
				return ValidationStatus.Pass;
			}

			return ValidationStatus.Unknown;
		}
	}

}
