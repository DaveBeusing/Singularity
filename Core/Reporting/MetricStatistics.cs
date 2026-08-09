// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Reporting;

public sealed class MetricStatistics
{
	public long SampleCount { get; init; }
	public double Minimum { get; init; }
	public double Average { get; init; }
	public double Maximum { get; init; }
}

internal sealed class MetricAccumulator
{
	private long sampleCount;
	private double sum;
	private double minimum = double.MaxValue;
	private double maximum = double.MinValue;

	public void Add(double value)
	{
		if (!double.IsFinite(value))
			return;

		sampleCount++;
		sum += value;
		minimum = Math.Min(minimum, value);
		maximum = Math.Max(maximum, value);
	}

	public MetricStatistics? Snapshot()
	{
		if (sampleCount == 0)
			return null;

		return new MetricStatistics
		{
			SampleCount = sampleCount,
			Minimum = minimum,
			Average = sum / sampleCount,
			Maximum = maximum
		};
	}
}
