// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Models;

public sealed class GpuInventory
{
	public string Identifier { get; set; } = string.Empty;
	public long? AdapterLuid { get; set; }
	public int AdapterIndex { get; set; }
	public string Vendor { get; set; } = "Unknown";
	public uint? VendorId { get; set; }
	public uint? DeviceId { get; set; }
	public uint? SubsystemId { get; set; }
	public uint? Revision { get; set; }
	public ulong? DedicatedVideoMemoryBytes { get; set; }
	public bool IsNvidia { get; set; } = true;
	public bool IsDirect3D12Capable { get; set; } = true;
	public string Name { get; set; } = "Unknown";

	public string Vram { get; set; } = "Unknown";
	public string Temperature { get; set; } = "Unknown";

	public string PcieGenerationCurrent { get; set; } = "Unknown";
	public string PcieGenerationMax { get; set; } = "Unknown";

	public string PcieWidthCurrent { get; set; } = "Unknown";
	public string PcieWidthMax { get; set; } = "Unknown";

	public string Details { get; set; } = "Unknown";
}
