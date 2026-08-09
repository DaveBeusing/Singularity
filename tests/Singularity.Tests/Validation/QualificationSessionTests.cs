// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.Monitoring;

namespace Singularity.Tests.Validation;

public sealed class QualificationSessionTests
{
	[Fact]
	public void Complete_FreezesResultAndTelemetryStatistics()
	{
		QualificationSession session = new();
		session.Start(QualificationProfiles.Quick);
		session.RecordTelemetry(new SystemSnapshot { CpuLoadPercent = 40, UsedPhysicalMemoryPercent = 25 });
		session.RecordTelemetry(new SystemSnapshot { CpuLoadPercent = 80, UsedPhysicalMemoryPercent = 75 });

		session.Complete(ValidationStatus.Pass);

		Assert.Equal(QualificationSessionState.Completed, session.State);
		Assert.Equal(ValidationStatus.Pass, session.Result);
		Assert.True(session.CanBeRecorded);
		Assert.Equal("Quick", session.Profile.Name);
		Assert.Equal(60, session.TelemetryStatistics.CpuLoadPercent?.Average);
		Assert.Equal(50, session.TelemetryStatistics.SystemMemoryUsagePercent?.Average);
	}

	[Fact]
	public void Reset_ClearsCompletedSession()
	{
		QualificationSession session = new();
		session.Start(QualificationProfiles.BurnIn);
		session.Fail();

		session.Reset();

		Assert.Equal(QualificationSessionState.Idle, session.State);
		Assert.Null(session.StartTime);
		Assert.Null(session.EndTime);
		Assert.False(session.CanBeRecorded);
		Assert.Equal(ValidationStatus.Unknown, session.Result);
	}
}
