// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Qualification;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;

namespace Singularity.Application.Persistence;

internal sealed class QualificationArchiveDocument
{
	public const int MinimumSupportedSchemaVersion = 1;
	public const int CurrentSchemaVersion = 2;

	public int SchemaVersion { get; init; } = CurrentSchemaVersion;
	public IReadOnlyList<QualificationArchiveRecordDto> Records { get; init; } =
		Array.Empty<QualificationArchiveRecordDto>();
}

internal sealed class QualificationArchiveRecordDto
{
	public DateTime StartedAt { get; init; }
	public DateTime FinishedAt { get; init; }
	public TimeSpan Duration { get; init; }
	public ValidationStatus Result { get; init; } = ValidationStatus.Unknown;
	public QualificationExecutionMode ExecutionMode { get; init; } =
		QualificationExecutionMode.Unknown;
	public string ProfileName { get; init; } = string.Empty;
	public SessionTelemetryStatistics TelemetryStatistics { get; init; } =
		SessionTelemetryStatistics.Empty;
	public QualificationTelemetryTimeline TelemetryTimeline { get; init; } =
		QualificationTelemetryTimeline.Empty;
	public IReadOnlyList<GpuQualificationEvidence> GpuEvidence { get; init; } =
		Array.Empty<GpuQualificationEvidence>();
	public QualificationReport? Report { get; init; }

	public static QualificationArchiveRecordDto FromRecord(QualificationRecord record)
	{
		ArgumentNullException.ThrowIfNull(record);

		return new QualificationArchiveRecordDto
		{
			StartedAt = record.StartedAt,
			FinishedAt = record.FinishedAt,
			Duration = record.Duration,
			Result = record.Result,
			ExecutionMode = record.ExecutionMode,
			ProfileName = record.ProfileName,
			TelemetryStatistics = record.TelemetryStatistics,
			TelemetryTimeline = record.TelemetryTimeline,
			GpuEvidence = record.GpuEvidence,
			Report = record.Report
		};
	}

	public QualificationRecord ToRecord()
	{
		return new QualificationRecord
		{
			StartedAt = StartedAt,
			FinishedAt = FinishedAt,
			Duration = Duration,
			Result = Result,
			ExecutionMode = ExecutionMode,
			ProfileName = ProfileName,
			TelemetryStatistics = TelemetryStatistics,
			TelemetryTimeline = TelemetryTimeline,
			GpuEvidence = GpuEvidence,
			Report = Report
		};
	}
}
