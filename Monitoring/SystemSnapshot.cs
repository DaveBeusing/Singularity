// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Monitoring;

public sealed class SystemSnapshot
{
	public double CpuLoadPercent { get; set; }

	public long TotalPhysicalMemoryMb { get; set; }
	public long AvailablePhysicalMemoryMb { get; set; }
	public long UsedPhysicalMemoryMb { get; set; }
	public double UsedPhysicalMemoryPercent { get; set; }

	public bool GpuTelemetryAvailable { get; set; }
	public double GpuLoadPercent { get; set; }
	public double GpuMemoryLoadPercent { get; set; }
	public int GpuTemperatureCelsius { get; set; }
	public string GpuTelemetryStatus { get; set; } = "Unavailable";

	public double ProcessCpuPercent { get; set; }
	public long ProcessMemoryMb { get; set; }

}