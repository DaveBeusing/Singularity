// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Monitoring;

public sealed class GpuTelemetrySnapshot
{
	public bool IsAvailable { get; init; }

	public double LoadPercent { get; init; }
	public double MemoryControllerLoadPercent { get; init; }

	public ulong MemoryTotalBytes { get; init; }
	public ulong MemoryUsedBytes { get; init; }
	public ulong MemoryFreeBytes { get; init; }

	public double MemoryUsedPercent => MemoryTotalBytes > 0 ? MemoryUsedBytes / (double)MemoryTotalBytes * 100.0 : 0;

	public int TemperatureCelsius { get; init; }

	public bool PowerAvailable { get; init; }
	public double PowerWatts { get; init; }

	public string Status { get; init; } = "Unavailable";

}