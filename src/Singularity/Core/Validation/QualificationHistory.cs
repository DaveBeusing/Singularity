// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;

namespace Singularity.Core.Validation;

public sealed class QualificationHistory
{
	private const int MaximumRecords = 10;
	private readonly List<QualificationRecord> records = [];

	public IReadOnlyList<QualificationRecord> Records => records;

	public QualificationRecord? Add(QualificationSession session, QualificationReport? report = null)
	{
		ArgumentNullException.ThrowIfNull(session);

		if (session.StartTime is null || session.EndTime is null)
			return null;

		QualificationRecord record = new()
		{
			StartedAt = session.StartTime.Value,
			FinishedAt = session.EndTime.Value,
			Duration = session.Duration,
			Result = session.Result,
			ExecutionMode = session.ExecutionMode,
			ProfileName = session.Profile.Name,
			TelemetryStatistics = session.TelemetryStatistics,
			TelemetryTimeline = report?.TelemetryTimeline ?? session.TelemetryTimeline,
			GpuEvidence = report?.GpuEvidence ??
				CreateUnavailableGpuEvidence(session.TelemetryStatistics.Gpus),
			Report = report
		};

		records.Insert(0, record);

		while (records.Count > MaximumRecords)
			records.RemoveAt(records.Count - 1);

		return record;
	}

	public void Clear()
	{
		records.Clear();
	}

	private static IReadOnlyList<GpuQualificationEvidence> CreateUnavailableGpuEvidence(
		IReadOnlyList<GpuTelemetryStatistics> statistics)
	{
		return Array.AsReadOnly(
			statistics
				.Select(item => new GpuQualificationEvidence
				{
					Identifier = item.Identifier,
					Name = string.IsNullOrWhiteSpace(item.Name) ? item.Identifier : item.Name,
					Result = ValidationStatus.Unknown,
					ValidationMessage = "Validation unavailable",
					TelemetryAvailable = item.TelemetryAvailable,
					TelemetryStatistics = item
				})
				.ToArray());
	}
}
