// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

public sealed class QualificationHistory
{
	private readonly List<QualificationRecord> records = [];

	public IReadOnlyList<QualificationRecord> Records => records;

	public void Add(QualificationSession session)
	{
		if (session.StartTime is null || session.EndTime is null)
			return;

		records.Insert(
			0,
			new QualificationRecord
			{
				StartedAt = session.StartTime.Value,
				FinishedAt = session.EndTime.Value,
				Duration = session.Duration,
				Result = session.Result
			});

		while (records.Count > 10)
		{
			records.RemoveAt(records.Count - 1);
		}
	}

	public void Clear()
	{
		records.Clear();
	}

}