// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.Monitoring.Models;

namespace Singularity.Tests.Reporting;

public sealed class QualificationTelemetryTimelineTests
{
	[Fact]
	public void Complete_FreezesChronologicalTimelineAndLifecycleMarkers()
	{
		QualificationSession session = new();
		session.Start(QualificationProfiles.Quick);
		for (int second = 0; second < 10; second++)
		{
			session.RecordTelemetry(
				new SystemSnapshot
				{
					CpuLoadPercent = second * 10,
					UsedPhysicalMemoryPercent = 20 + second
				},
				TimeSpan.FromSeconds(second));
		}

		session.Complete(ValidationStatus.Pass);

		QualificationTelemetryTimeline timeline = session.TelemetryTimeline;
		Assert.NotEmpty(timeline.Points);
		Assert.True(timeline.Points.Count <= QualificationTelemetryTimeline.DefaultMaximumPoints);
		Assert.Equal(TimeSpan.Zero, timeline.Points[0].Elapsed);
		Assert.Equal(TimeSpan.FromSeconds(9), timeline.Points[^1].Elapsed);
		Assert.True(timeline.Points.Zip(timeline.Points.Skip(1), (left, right) => left.Elapsed <= right.Elapsed).All(value => value));
		Assert.Equal(QualificationTimelineEventKind.Start, timeline.Events[0].Kind);
		Assert.Equal(QualificationTimelineEventKind.Completed, timeline.Events[^1].Kind);
	}

	[Fact]
	public void BurnInSimulation_RemainsBoundedAndRetainsEndpointsAndExtrema()
	{
		QualificationSession session = new();
		session.Start(QualificationProfiles.BurnIn, selectedGpuIdentifiers: ["GPU-A", "GPU-B"]);

		for (int sample = 0; sample <= 7200; sample++)
		{
			double cpu = sample == 123 ? 0 : sample == 4567 ? 100 : 50;
			session.RecordTelemetry(
				CreateSnapshot(cpu, sample),
				TimeSpan.FromMilliseconds(sample * 500L));
		}

		session.Complete(ValidationStatus.Pass);

		QualificationTelemetryTimeline timeline = session.TelemetryTimeline;
		Assert.InRange(timeline.Points.Count, 2, QualificationTelemetryTimeline.DefaultMaximumPoints);
		Assert.Equal(TimeSpan.Zero, timeline.Points[0].Elapsed);
		Assert.Equal(TimeSpan.FromHours(1), timeline.Points[^1].Elapsed);
		Assert.Contains(timeline.Points, point => point.CpuLoadPercent == 0);
		Assert.Contains(timeline.Points, point => point.CpuLoadPercent == 100);
		Assert.Contains(timeline.Points, point =>
			point.Gpus.Any(gpu => gpu.Identifier == "GPU-B" && gpu.LoadPercent is null));
	}

	[Fact]
	public void LongRunBeyondExpectedDuration_AdaptsSamplingWithoutGrowingPastBudget()
	{
		QualificationSession session = new();
		session.Start(QualificationProfiles.Quick);

		for (int second = 0; second <= 14_400; second++)
		{
			session.RecordTelemetry(
				new SystemSnapshot
				{
					CpuLoadPercent = second % 101,
					UsedPhysicalMemoryPercent = 40 + second % 20
				},
				TimeSpan.FromSeconds(second));
		}

		session.Complete(ValidationStatus.Pass);

		QualificationTelemetryTimeline timeline = session.TelemetryTimeline;
		Assert.True(timeline.Points.Count <= QualificationTelemetryTimeline.DefaultMaximumPoints);
		Assert.Equal(TimeSpan.FromHours(4), timeline.Points[^1].Elapsed);
		Assert.True(timeline.SamplingInterval > TimeSpan.FromMilliseconds(500));
	}

	[Fact]
	public void UnavailableGpuMetrics_AreRepresentedAsGapsRatherThanZeros()
	{
		QualificationSession session = new();
		session.Start(QualificationProfiles.Quick, selectedGpuIdentifiers: ["GPU-A"]);

		session.RecordTelemetry(CreateGpuSnapshot(0, available: true), TimeSpan.Zero);
		session.RecordTelemetry(CreateGpuSnapshot(0, available: false), TimeSpan.FromSeconds(1));
		session.RecordTelemetry(CreateGpuSnapshot(0, available: true), TimeSpan.FromSeconds(2));
		session.Complete(ValidationStatus.Pass);

		QualificationTelemetryGpuPoint gap = session.TelemetryTimeline.Points
			.Single(point => point.Elapsed == TimeSpan.FromSeconds(1))
			.Gpus
			.Single();

		Assert.Null(gap.LoadPercent);
		Assert.Null(gap.TemperatureCelsius);
		Assert.Null(gap.PowerWatts);
		Assert.Null(gap.VramUsagePercent);
	}

	private static SystemSnapshot CreateSnapshot(double cpuLoad, int sample)
	{
		return new SystemSnapshot
		{
			CpuLoadPercent = cpuLoad,
			UsedPhysicalMemoryPercent = 45 + sample % 10,
			GpuTelemetrySnapshots =
			[
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-A",
					Name = "GPU A",
					IsAvailable = true,
					LoadPercent = sample % 101,
					TemperatureCelsius = 50 + sample % 20,
					PowerAvailable = true,
					PowerWatts = 100 + sample % 50,
					MemoryTotalBytes = 100,
					MemoryUsedBytes = (ulong)(sample % 101)
				},
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-B",
					Name = "GPU B",
					IsAvailable = sample % 20 != 0,
					LoadPercent = 70,
					TemperatureCelsius = 60,
					PowerAvailable = true,
					PowerWatts = 150,
					MemoryTotalBytes = 100,
					MemoryUsedBytes = 50
				}
			]
		};
	}

	private static SystemSnapshot CreateGpuSnapshot(int load, bool available)
	{
		return new SystemSnapshot
		{
			GpuTelemetrySnapshots =
			[
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-A",
					Name = "GPU A",
					IsAvailable = available,
					LoadPercent = load,
					TemperatureCelsius = 55,
					PowerAvailable = available,
					PowerWatts = 120,
					MemoryTotalBytes = available ? 100UL : 0,
					MemoryUsedBytes = available ? 50UL : 0
				}
			]
		};
	}
}
