// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

using Singularity.Core.Qualification;
using Singularity.Core.Reporting;
using Singularity.Monitoring.Models;

public sealed class QualificationSession
{
	private SessionTelemetryCollector telemetryCollector = new();
	private QualificationTelemetryTimelineCollector timelineCollector = new();

	public QualificationSessionState State { get; private set; } =
		QualificationSessionState.Idle;

	public DateTime? StartTime { get; private set; }

	public DateTime? EndTime { get; private set; }

	public ValidationStatus Result { get; private set; } =
		ValidationStatus.Unknown;

	public SessionTelemetryStatistics TelemetryStatistics { get; private set; } =
		SessionTelemetryStatistics.Empty;

	public QualificationTelemetryTimeline TelemetryTimeline { get; private set; } =
		QualificationTelemetryTimeline.Empty;

	public QualificationProfile Profile { get; private set; } =
		QualificationProfiles.Standard;

	public QualificationExecutionMode ExecutionMode { get; private set; } =
		QualificationExecutionMode.Unknown;

	public IReadOnlyList<string> SelectedGpuIdentifiers { get; private set; } =
		Array.Empty<string>();

	public TimeSpan Duration
	{
		get
		{
			if (StartTime is null)
				return TimeSpan.Zero;

			if (State == QualificationSessionState.Running)
				return DateTime.Now - StartTime.Value;

			if (EndTime is null)
				return TimeSpan.Zero;

			return EndTime.Value - StartTime.Value;
		}
	}

	public bool CanBeRecorded =>
		StartTime is not null &&
		EndTime is not null &&
		State is QualificationSessionState.Completed or QualificationSessionState.Failed;

	public void Start(
		QualificationProfile profile,
		QualificationExecutionMode executionMode = QualificationExecutionMode.Manual,
		IReadOnlyList<string>? selectedGpuIdentifiers = null)
	{
		QualificationProfileValidator.EnsureValid(profile);
		QualificationProfile effectiveProfile = profile.Snapshot();

		SelectedGpuIdentifiers = Array.AsReadOnly(
			(selectedGpuIdentifiers ?? Array.Empty<string>())
				.Where(identifier => !string.IsNullOrWhiteSpace(identifier))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray());

		State = QualificationSessionState.Running;
		StartTime = DateTime.Now;
		EndTime = null;
		Result = ValidationStatus.Unknown;
		telemetryCollector = new SessionTelemetryCollector(SelectedGpuIdentifiers);
		timelineCollector = new QualificationTelemetryTimelineCollector(
			effectiveProfile.RecommendedDuration,
			SelectedGpuIdentifiers);
		timelineCollector.AddEvent(TimeSpan.Zero, QualificationTimelineEventKind.Start, "Qualification started");
		TelemetryStatistics = SessionTelemetryStatistics.Empty;
		TelemetryTimeline = QualificationTelemetryTimeline.Empty;
		Profile = effectiveProfile;
		ExecutionMode = executionMode;
	}

	public void RecordTelemetry(SystemSnapshot snapshot, TimeSpan? elapsed = null)
	{
		ArgumentNullException.ThrowIfNull(snapshot);
		if (State != QualificationSessionState.Running)
			return;

		telemetryCollector.Add(snapshot);
		timelineCollector.Add(snapshot, elapsed ?? Duration);
	}

	internal void RecordTimelineEvent(
		QualificationTimelineEventKind kind,
		string label,
		TimeSpan? elapsed = null)
	{
		if (State != QualificationSessionState.Running)
			return;

		timelineCollector.AddEvent(
			elapsed ?? Max(Duration, timelineCollector.LastElapsed),
			kind,
			label);
	}

	public void Complete(ValidationStatus result)
	{
		if (State != QualificationSessionState.Running)
			return;

		RecordTimelineEvent(QualificationTimelineEventKind.Completed, "Qualification completed");
		State = QualificationSessionState.Completed;
		EndTime = DateTime.Now;
		Result = result;
		TelemetryStatistics = telemetryCollector.Snapshot();
		TelemetryTimeline = timelineCollector.Snapshot();
	}

	public void Fail()
	{
		if (State != QualificationSessionState.Running)
			return;

		RecordTimelineEvent(QualificationTimelineEventKind.Failed, "Qualification failed");
		State = QualificationSessionState.Failed;
		EndTime = DateTime.Now;
		Result = ValidationStatus.Fail;
		TelemetryStatistics = telemetryCollector.Snapshot();
		TelemetryTimeline = timelineCollector.Snapshot();
	}

	public void Reset()
	{
		State = QualificationSessionState.Idle;
		StartTime = null;
		EndTime = null;
		Result = ValidationStatus.Unknown;
		SelectedGpuIdentifiers = Array.Empty<string>();
		telemetryCollector = new SessionTelemetryCollector();
		timelineCollector = new QualificationTelemetryTimelineCollector();
		TelemetryStatistics = SessionTelemetryStatistics.Empty;
		TelemetryTimeline = QualificationTelemetryTimeline.Empty;
		Profile = QualificationProfiles.Standard;
		ExecutionMode = QualificationExecutionMode.Unknown;
	}

	private static TimeSpan Max(TimeSpan left, TimeSpan right) =>
		left >= right ? left : right;
}
