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
	private DateTime? selectedStartedAt;
	private DateTime? latestStartedAt;

	public ReportsWorkspaceSnapshot CreateSnapshot(
		QualificationHistory history,
		bool inventoryAvailable)
	{
		ArgumentNullException.ThrowIfNull(history);

		IReadOnlyList<QualificationRecord> records = history.Records;
		if (records.Count == 0)
		{
			selectedStartedAt = null;
			latestStartedAt = null;
			return ReportsWorkspaceSnapshot.Empty;
		}

		DateTime newest = records[0].StartedAt;
		if (latestStartedAt != newest)
		{
			latestStartedAt = newest;
			selectedStartedAt = newest;
		}

		int selectedIndex = FindSelectedIndex(records);
		if (selectedIndex < 0)
		{
			selectedIndex = 0;
			selectedStartedAt = records[0].StartedAt;
		}

		QualificationRecord selectedRecord = records[selectedIndex];
		QualificationReport? selectedReport = selectedRecord.Report;

		return new ReportsWorkspaceSnapshot(
			records,
			selectedIndex,
			selectedRecord,
			selectedReport,
			selectedReport is not null && inventoryAvailable);
	}

	public ReportsWorkspaceSnapshot Select(
		int index,
		QualificationHistory history,
		bool inventoryAvailable)
	{
		ArgumentNullException.ThrowIfNull(history);

		if (index < 0 || index >= history.Records.Count)
			return CreateSnapshot(history, inventoryAvailable);

		selectedStartedAt = history.Records[index].StartedAt;
		latestStartedAt ??= history.Records[0].StartedAt;
		return CreateSnapshot(history, inventoryAvailable);
	}

	private int FindSelectedIndex(IReadOnlyList<QualificationRecord> records)
	{
		if (selectedStartedAt is null)
			return -1;

		for (int index = 0; index < records.Count; index++)
		{
			if (records[index].StartedAt == selectedStartedAt.Value)
				return index;
		}

		return -1;
	}
}
