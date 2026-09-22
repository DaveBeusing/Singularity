// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Qualification;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;

namespace Singularity.Tests.Application;

public sealed class ResultsWorkspaceStateTests
{
	[Fact]
	public void Create_WhenHistoryIsEmpty_ReturnsExplicitEmptyState()
	{
		ResultsWorkspaceSnapshot snapshot = ResultsWorkspaceState.Create(new QualificationHistory());

		Assert.False(snapshot.HasResult);
		Assert.Equal(ValidationStatus.Unknown, snapshot.OverallStatus);
	}

	[Fact]
	public void Create_MapsLatestCompletedReport()
	{
		QualificationHistory history = new();
		QualificationSession session = new();
		session.Start(QualificationProfiles.Standard, QualificationExecutionMode.Automated);
		session.Complete(ValidationStatus.Warning);
		QualificationReport report = new()
		{
			StartedAt = session.StartTime!.Value,
			FinishedAt = session.EndTime!.Value,
			Duration = session.Duration,
			Profile = session.Profile,
			ExecutionMode = session.ExecutionMode,
			CpuResult = ValidationStatus.Pass,
			MemoryResult = ValidationStatus.Warning,
			GpuResult = ValidationStatus.Unknown,
			OverallResult = ValidationStatus.Warning,
			TelemetryStatistics = new SessionTelemetryStatistics
			{
				CpuLoadPercent = new MetricStatistics
				{
					SampleCount = 3,
					Minimum = 80,
					Average = 90,
					Maximum = 100
				}
			}
		};
		history.Add(session, report);

		ResultsWorkspaceSnapshot snapshot = ResultsWorkspaceState.Create(history);

		Assert.True(snapshot.HasResult);
		Assert.Equal("Standard", snapshot.ProfileName);
		Assert.Equal(QualificationExecutionMode.Automated, snapshot.ExecutionMode);
		Assert.Equal(ValidationStatus.Warning, snapshot.OverallStatus);
		Assert.Equal(ValidationStatus.Pass, snapshot.CpuStatus);
		Assert.Equal(90, snapshot.TelemetryStatistics.CpuLoadPercent!.Average);
	}
}
