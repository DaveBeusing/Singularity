// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Monitoring;

public sealed class SystemSnapshot
{
	public double CpuLoadPercent { get; set; }
	public bool CpuTemperatureAvailable { get; set; }
	public double CpuTemperatureCelsius { get; set; }
	public string CpuTemperatureStatus { get; set; } = "Unavailable";

	public long TotalPhysicalMemoryMb { get; set; }
	public long AvailablePhysicalMemoryMb { get; set; }
	public long UsedPhysicalMemoryMb { get; set; }
	public double UsedPhysicalMemoryPercent { get; set; }

	public bool GpuTelemetryAvailable { get; set; }
	public double GpuLoadPercent { get; set; }
	public double GpuMemoryControllerLoadPercent { get; set; }
	public double GpuMemoryUsedPercent { get; set; }
	public long GpuMemoryUsedMb { get; set; }
	public long GpuMemoryTotalMb { get; set; }
	public int GpuTemperatureCelsius { get; set; }
	public bool GpuPowerAvailable { get; set; }
	public double GpuPowerWatts { get; set; }
	public string GpuTelemetryStatus { get; set; } = "Unavailable";
	public IReadOnlyList<GpuTelemetrySnapshot> GpuTelemetrySnapshots { get; set; } = Array.Empty<GpuTelemetrySnapshot>();

	public double ProcessCpuPercent { get; set; }
	public long ProcessMemoryMb { get; set; }

	internal SystemSnapshot Copy()
	{
		SystemSnapshot copy = (SystemSnapshot)MemberwiseClone();
		copy.GpuTelemetrySnapshots = Array.AsReadOnly(GpuTelemetrySnapshots.ToArray());
		return copy;
	}

	internal void ApplyGpuSnapshots(IReadOnlyList<GpuTelemetrySnapshot> gpuSnapshots)
	{
		GpuTelemetrySnapshots = Array.AsReadOnly(gpuSnapshots.ToArray());
		if (gpuSnapshots.Count == 0)
		{
			GpuTelemetryAvailable = false;
			GpuPowerAvailable = false;
			GpuTelemetryStatus = "GPU telemetry unavailable";
			return;
		}

		GpuTelemetrySnapshot gpu = gpuSnapshots[0];
		GpuTelemetryAvailable = gpu.IsAvailable;
		GpuLoadPercent = gpu.LoadPercent;
		GpuMemoryControllerLoadPercent = gpu.MemoryControllerLoadPercent;
		GpuMemoryUsedPercent = gpu.MemoryUsedPercent;
		GpuMemoryUsedMb = (long)(gpu.MemoryUsedBytes / 1024 / 1024);
		GpuMemoryTotalMb = (long)(gpu.MemoryTotalBytes / 1024 / 1024);
		GpuTemperatureCelsius = gpu.TemperatureCelsius;
		GpuPowerAvailable = gpu.PowerAvailable;
		GpuPowerWatts = gpu.PowerWatts;
		GpuTelemetryStatus = gpu.Status;
	}
}
