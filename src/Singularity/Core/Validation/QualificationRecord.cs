// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

public sealed class QualificationRecord
{
	public DateTime StartedAt { get; init; }
	public DateTime FinishedAt { get; init; }
	public TimeSpan Duration { get; init; }
	public ValidationStatus Result { get; init; } = ValidationStatus.Unknown;

	public string DisplayResult => Result.ToString().ToUpperInvariant();

	public string DisplayDuration => Duration.ToString(@"hh\:mm\:ss");

	public string DisplayStarted => StartedAt.ToString("HH:mm:ss");

}