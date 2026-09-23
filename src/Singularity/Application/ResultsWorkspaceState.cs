// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Qualification;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;

namespace Singularity.Application;

public sealed record ResultsWorkspaceSnapshot(
	bool HasResult,
	ValidationStatus OverallStatus,
	string ProfileName,
	QualificationExecutionMode ExecutionMode,
	DateTime? StartedAt,
	DateTime? FinishedAt,
	TimeSpan Duration,
	ValidationStatus CpuStatus,
	ValidationStatus MemoryStatus,
	ValidationStatus GpuStatus,
	SessionTelemetryStatistics TelemetryStatistics,
	QualificationTelemetryTimeline TelemetryTimeline,
	IReadOnlyList<GpuQualificationEvidence> GpuEvidence)
{
	public static ResultsWorkspaceSnapshot Empty { get; } = new(
		HasResult: false,
		OverallStatus: ValidationStatus.Unknown,
		ProfileName: string.Empty,
		ExecutionMode: QualificationExecutionMode.Unknown,
		StartedAt: null,
		FinishedAt: null,
		Duration: TimeSpan.Zero,
		CpuStatus: ValidationStatus.Unknown,
		MemoryStatus: ValidationStatus.Unknown,
		GpuStatus: ValidationStatus.Unknown,
		TelemetryStatistics: SessionTelemetryStatistics.Empty,
		TelemetryTimeline: QualificationTelemetryTimeline.Empty,
		GpuEvidence: Array.Empty<GpuQualificationEvidence>());
}

public static class ResultsWorkspaceState
{
	public static ResultsWorkspaceSnapshot Create(QualificationHistory history)
	{
		ArgumentNullException.ThrowIfNull(history);
		return Create(history.Records);
	}

	public static ResultsWorkspaceSnapshot Create(IReadOnlyList<QualificationRecord> records)
	{
		ArgumentNullException.ThrowIfNull(records);

		if (records.Count == 0)
			return ResultsWorkspaceSnapshot.Empty;

		QualificationRecord record = records[0];
		QualificationReport? report = record.Report;

		return new ResultsWorkspaceSnapshot(
			HasResult: true,
			OverallStatus: record.Result,
			ProfileName: string.IsNullOrWhiteSpace(record.ProfileName)
				? report?.Profile.Name ?? "Unavailable"
				: record.ProfileName,
			ExecutionMode: record.ExecutionMode,
			StartedAt: record.StartedAt,
			FinishedAt: record.FinishedAt,
			Duration: record.Duration,
			CpuStatus: report?.CpuResult ?? ValidationStatus.Unknown,
			MemoryStatus: report?.MemoryResult ?? ValidationStatus.Unknown,
			GpuStatus: report?.GpuResult ?? ValidationStatus.Unknown,
			TelemetryStatistics: report?.TelemetryStatistics ?? record.TelemetryStatistics,
			TelemetryTimeline: report?.TelemetryTimeline ?? record.TelemetryTimeline,
			GpuEvidence: report?.GpuEvidence ?? record.GpuEvidence);
	}
}
