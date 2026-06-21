// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.Core.Reporting;

public sealed class QualificationReport
{
	public DateTime StartedAt { get; init; }

	public DateTime FinishedAt { get; init; }

	public TimeSpan Duration { get; init; }

	public ValidationStatus CpuResult { get; init; } = ValidationStatus.Unknown;

	public ValidationStatus MemoryResult { get; init; } = ValidationStatus.Unknown;

	public ValidationStatus OverallResult { get; init; } = ValidationStatus.Unknown;

}