// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
namespace Singularity.Monitoring;

public sealed class HardwareInfo
{
	public string Mainboard { get; set; } = "Unknown";
	public string MainboardDetails { get; set; } = "Unknown";
	public string Cpu { get; set; } = "Unknown";
	public string CpuDetails { get; set; } = "Unknown";
	public string Gpu { get; set; } = "Unknown";
	public string GpuVram { get; set; } = "Unknown";
	public string GpuTemperature { get; set; } = "Unknown";
	public string GpuPcieCurrent { get; set; } = "Unknown";
	public string GpuPcieMax { get; set; } = "Unknown";
	public List<MemoryModuleInfo> MemoryModules { get; set; } = new();
}