// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

using Singularity.Core.Reporting;
using Singularity.Monitoring.Models;

public sealed class QualificationSession
{
	private SessionTelemetryCollector telemetryCollector = new();

	public QualificationSessionState State { get; private set; } =
		QualificationSessionState.Idle;

	public DateTime? StartTime { get; private set; }

	public DateTime? EndTime { get; private set; }

	public ValidationStatus Result { get; private set; } =
		ValidationStatus.Unknown;

	public SessionTelemetryStatistics TelemetryStatistics { get; private set; } =
		SessionTelemetryStatistics.Empty;

	public QualificationProfile Profile { get; private set; } =
		QualificationProfiles.Standard;

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

	public void Start(QualificationProfile profile)
	{
		State = QualificationSessionState.Running;
		StartTime = DateTime.Now;
		EndTime = null;
		Result = ValidationStatus.Unknown;
		telemetryCollector = new SessionTelemetryCollector();
		TelemetryStatistics = SessionTelemetryStatistics.Empty;
		Profile = profile;
	}

	public void RecordTelemetry(SystemSnapshot snapshot)
	{
		if (State == QualificationSessionState.Running)
			telemetryCollector.Add(snapshot);
	}

	public void Complete(ValidationStatus result)
	{
		if (State != QualificationSessionState.Running)
			return;

		State = QualificationSessionState.Completed;
		EndTime = DateTime.Now;
		Result = result;
		TelemetryStatistics = telemetryCollector.Snapshot();
	}

	public void Fail()
	{
		State = QualificationSessionState.Failed;
		EndTime = DateTime.Now;
		Result = ValidationStatus.Fail;
		TelemetryStatistics = telemetryCollector.Snapshot();
	}

	public void Reset()
	{
		State = QualificationSessionState.Idle;
		StartTime = null;
		EndTime = null;
		Result = ValidationStatus.Unknown;
		telemetryCollector = new SessionTelemetryCollector();
		TelemetryStatistics = SessionTelemetryStatistics.Empty;
		Profile = QualificationProfiles.Standard;
	}
}
