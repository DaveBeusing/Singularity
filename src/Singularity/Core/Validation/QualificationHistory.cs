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

	public void Add(QualificationSession session, QualificationReport? report = null)
	{
		ArgumentNullException.ThrowIfNull(session);

		if (session.StartTime is null || session.EndTime is null)
			return;

		records.Insert(
			0,
			new QualificationRecord
			{
				StartedAt = session.StartTime.Value,
				FinishedAt = session.EndTime.Value,
				Duration = session.Duration,
				Result = session.Result,
				ExecutionMode = session.ExecutionMode,
				ProfileName = session.Profile.Name,
				Report = report
			});

		while (records.Count > MaximumRecords)
			records.RemoveAt(records.Count - 1);
	}

	public void Clear()
	{
		records.Clear();
	}
}
