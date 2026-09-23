// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Qualification;
using Singularity.Core.Reporting;

namespace Singularity.Core.Validation;

public sealed class QualificationRecord
{
	public DateTime StartedAt { get; init; }
	public DateTime FinishedAt { get; init; }
	public TimeSpan Duration { get; init; }
	public ValidationStatus Result { get; init; } = ValidationStatus.Unknown;
	public QualificationExecutionMode ExecutionMode { get; init; } = QualificationExecutionMode.Unknown;
	public string ProfileName { get; init; } = string.Empty;
	public QualificationProfile Profile { get; init; } =
		QualificationProfiles.Standard.Snapshot();
	public SessionTelemetryStatistics TelemetryStatistics { get; init; } =
		SessionTelemetryStatistics.Empty;
	public QualificationTelemetryTimeline TelemetryTimeline { get; init; } =
		QualificationTelemetryTimeline.Empty;
	public IReadOnlyList<GpuQualificationEvidence> GpuEvidence { get; init; } =
		Array.Empty<GpuQualificationEvidence>();
	public QualificationReport? Report { get; init; }
}
