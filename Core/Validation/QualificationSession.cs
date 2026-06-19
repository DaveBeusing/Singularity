// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

public sealed class QualificationSession
{
	public QualificationSessionState State { get; private set; } = QualificationSessionState.Idle;

	public DateTime? StartTime { get; private set; }

	public DateTime? EndTime { get; private set; }

	public ValidationStatus Result { get; private set; } = ValidationStatus.Unknown;

	public TimeSpan Duration
	{
		get
		{
			if (StartTime is null)
			{
				return TimeSpan.Zero;
			}

			if (State == QualificationSessionState.Running)
			{
				return DateTime.Now - StartTime.Value;
			}

			if (EndTime is null)
			{
				return TimeSpan.Zero;
			}

			return EndTime.Value - StartTime.Value;
		}
	}

	public void Start()
	{
		State = QualificationSessionState.Running;

		StartTime = DateTime.Now;
		EndTime = null;

		Result = ValidationStatus.Unknown;
	}

	public void Complete(
		ValidationStatus result)
	{
		if (State != QualificationSessionState.Running)
		{
			return;
		}

		State = QualificationSessionState.Completed;

		EndTime = DateTime.Now;

		Result = result;
	}

	public void Fail()
	{
		State = QualificationSessionState.Failed;

		EndTime = DateTime.Now;

		Result = ValidationStatus.Fail;
	}

	public void Reset()
	{
		State = QualificationSessionState.Idle;

		StartTime = null;
		EndTime = null;

		Result = ValidationStatus.Unknown;
	}

}