// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;

namespace Singularity.Tests.Reporting;

public sealed class MetricStatisticsTests
{
	[Fact]
	public void Snapshot_ComputesStatisticsAndIgnoresNonFiniteValues()
	{
		MetricAccumulator accumulator = new();

		accumulator.Add(10);
		accumulator.Add(double.NaN);
		accumulator.Add(20);
		accumulator.Add(double.PositiveInfinity);
		accumulator.Add(30);

		MetricStatistics statistics = Assert.IsType<MetricStatistics>(accumulator.Snapshot());
		Assert.Equal(3, statistics.SampleCount);
		Assert.Equal(10, statistics.Minimum);
		Assert.Equal(20, statistics.Average);
		Assert.Equal(30, statistics.Maximum);
	}

	[Fact]
	public void Snapshot_ReturnsNullWithoutSamples()
	{
		MetricAccumulator accumulator = new();

		Assert.Null(accumulator.Snapshot());
	}
}
