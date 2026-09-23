// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Reporting;

public sealed class GpuTelemetryStatistics
{
	public string Identifier { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public long AvailableSampleCount { get; init; }
	public long UnavailableSampleCount { get; init; }
	public MetricStatistics? LoadPercent { get; init; }
	public MetricStatistics? TemperatureCelsius { get; init; }
	public MetricStatistics? PowerWatts { get; init; }
	public MetricStatistics? VramUsagePercent { get; init; }

	public bool TelemetryAvailable => AvailableSampleCount > 0;
}
