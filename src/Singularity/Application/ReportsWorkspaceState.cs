// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;
using Singularity.Core.Validation;

namespace Singularity.Application;

public sealed record ReportsWorkspaceSnapshot(
	IReadOnlyList<QualificationRecord> Records,
	int SelectedIndex,
	QualificationRecord? SelectedRecord,
	QualificationReport? SelectedReport,
	bool CanExport)
{
	public static ReportsWorkspaceSnapshot Empty { get; } = new(
		Array.Empty<QualificationRecord>(),
		-1,
		null,
		null,
		false);
}

public sealed class ReportsWorkspaceState
{
	private QualificationRecord? selectedRecord;
	private QualificationRecord? latestRecord;

	public ReportsWorkspaceSnapshot CreateSnapshot(
		QualificationHistory history,
		bool inventoryAvailable)
	{
		ArgumentNullException.ThrowIfNull(history);

		IReadOnlyList<QualificationRecord> records = history.Records;
		if (records.Count == 0)
		{
			selectedRecord = null;
			latestRecord = null;
			return ReportsWorkspaceSnapshot.Empty;
		}

		if (!ReferenceEquals(latestRecord, records[0]))
		{
			latestRecord = records[0];
			selectedRecord = records[0];
		}

		int selectedIndex = FindSelectedIndex(records);
		if (selectedIndex < 0)
		{
			selectedIndex = 0;
			selectedRecord = records[0];
		}

		QualificationRecord current = records[selectedIndex];
		QualificationReport? selectedReport = current.Report;

		return new ReportsWorkspaceSnapshot(
			records,
			selectedIndex,
			current,
			selectedReport,
			selectedReport is not null && inventoryAvailable);
	}

	public ReportsWorkspaceSnapshot Select(
		int index,
		QualificationHistory history,
		bool inventoryAvailable)
	{
		ArgumentNullException.ThrowIfNull(history);

		if (index >= 0 && index < history.Records.Count)
			selectedRecord = history.Records[index];

		latestRecord ??= history.Records.Count > 0 ? history.Records[0] : null;
		return CreateSnapshot(history, inventoryAvailable);
	}

	private int FindSelectedIndex(IReadOnlyList<QualificationRecord> records)
	{
		if (selectedRecord is null)
			return -1;

		for (int index = 0; index < records.Count; index++)
		{
			if (ReferenceEquals(records[index], selectedRecord))
				return index;
		}

		return -1;
	}
}
