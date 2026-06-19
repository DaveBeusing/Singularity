// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

public sealed class ValidationResult
{
	public ValidationStatus CpuStatus { get; init; }
	public ValidationStatus MemoryStatus { get; init; }

	public string CpuMessage { get; init; } = string.Empty;
	public string MemoryMessage { get; init; } = string.Empty;

	public bool IsSuccess => CpuStatus != ValidationStatus.Fail && MemoryStatus != ValidationStatus.Fail;

}