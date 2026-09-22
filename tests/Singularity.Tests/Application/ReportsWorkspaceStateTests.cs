// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;

namespace Singularity.Tests.Application;

public sealed class ReportsWorkspaceStateTests
{
	[Fact]
	public void CreateSnapshot_WhenHistoryIsEmpty_DisablesExport()
	{
		ReportsWorkspaceState state = new();

		ReportsWorkspaceSnapshot snapshot = state.CreateSnapshot(new QualificationHistory(), inventoryAvailable: true);

		Assert.Empty(snapshot.Records);
		Assert.Equal(-1, snapshot.SelectedIndex);
		Assert.False(snapshot.CanExport);
	}

	[Fact]
	public void Select_ChangesSelectedReportAndExportState()
	{
		QualificationHistory history = CreateHistory();
		ReportsWorkspaceState state = new();

		ReportsWorkspaceSnapshot initial = state.CreateSnapshot(history, inventoryAvailable: true);
		ReportsWorkspaceSnapshot selected = state.Select(1, history, inventoryAvailable: true);

		Assert.Equal(0, initial.SelectedIndex);
		Assert.Equal(1, selected.SelectedIndex);
		Assert.NotNull(selected.SelectedRecord);
		Assert.NotNull(selected.SelectedReport);
		Assert.True(selected.CanExport);
	}

	[Fact]
	public void CreateSnapshot_RequiresInventoryForExport()
	{
		QualificationHistory history = CreateHistory();
		ReportsWorkspaceState state = new();

		ReportsWorkspaceSnapshot snapshot = state.CreateSnapshot(history, inventoryAvailable: false);

		Assert.NotNull(snapshot.SelectedReport);
		Assert.False(snapshot.CanExport);
	}

	private static QualificationHistory CreateHistory()
	{
		QualificationHistory history = new();

		for (int index = 0; index < 2; index++)
		{
			QualificationSession session = new();
			session.Start(QualificationProfiles.Quick);
			session.Complete(ValidationStatus.Pass);
			QualificationReport report = new()
			{
				StartedAt = session.StartTime!.Value,
				FinishedAt = session.EndTime!.Value,
				Duration = session.Duration,
				Profile = session.Profile,
				OverallResult = ValidationStatus.Pass
			};
			history.Add(session, report);
			Thread.Sleep(1);
		}

		return history;
	}
}
