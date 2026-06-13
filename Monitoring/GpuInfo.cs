// Copyright (c) 2026 David Beusing
// Licensed under the MIT License.

namespace Singularity.Monitoring;

public sealed class GpuInfo
{
	public string Name { get; set; } = "Unknown";

	public string Vram { get; set; } = "Unknown";
	public string Temperature { get; set; } = "Unknown";

	public string PcieGenerationCurrent { get; set; } = "Unknown";
	public string PcieGenerationMax { get; set; } = "Unknown";

	public string PcieWidthCurrent { get; set; } = "Unknown";
	public string PcieWidthMax { get; set; } = "Unknown";

	public string Details { get; set; } = "Unknown";
}