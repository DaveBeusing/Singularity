// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Monitoring;

namespace Singularity.Core.Reporting;

public sealed class SessionTelemetryStatistics
{
	public static SessionTelemetryStatistics Empty { get; } = new();

	public MetricStatistics? CpuLoadPercent { get; init; }
	public MetricStatistics? GpuLoadPercent { get; init; }
	public MetricStatistics? GpuTemperatureCelsius { get; init; }
	public MetricStatistics? GpuPowerWatts { get; init; }
	public MetricStatistics? GpuVramUsagePercent { get; init; }
	public MetricStatistics? SystemMemoryUsagePercent { get; init; }
}

internal sealed class SessionTelemetryCollector
{
	private readonly MetricAccumulator cpuLoad = new();
	private readonly MetricAccumulator gpuLoad = new();
	private readonly MetricAccumulator gpuTemperature = new();
	private readonly MetricAccumulator gpuPower = new();
	private readonly MetricAccumulator gpuVram = new();
	private readonly MetricAccumulator systemMemory = new();

	public void Add(SystemSnapshot snapshot)
	{
		cpuLoad.Add(snapshot.CpuLoadPercent);
		systemMemory.Add(snapshot.UsedPhysicalMemoryPercent);

		if (!snapshot.GpuTelemetryAvailable)
			return;

		gpuLoad.Add(snapshot.GpuLoadPercent);
		gpuTemperature.Add(snapshot.GpuTemperatureCelsius);
		gpuVram.Add(snapshot.GpuMemoryUsedPercent);

		if (snapshot.GpuPowerAvailable)
			gpuPower.Add(snapshot.GpuPowerWatts);
	}

	public SessionTelemetryStatistics Snapshot()
	{
		return new SessionTelemetryStatistics
		{
			CpuLoadPercent = cpuLoad.Snapshot(),
			GpuLoadPercent = gpuLoad.Snapshot(),
			GpuTemperatureCelsius = gpuTemperature.Snapshot(),
			GpuPowerWatts = gpuPower.Snapshot(),
			GpuVramUsagePercent = gpuVram.Snapshot(),
			SystemMemoryUsagePercent = systemMemory.Snapshot()
		};
	}
}
