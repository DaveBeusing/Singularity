// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Monitoring;

public sealed class MemoryModuleInfo
{
	public string Slot { get; set; } = "Unknown";
	public string Manufacturer { get; set; } = "Unknown";
	public string PartNumber { get; set; } = "Unknown";
	public string Capacity { get; set; } = "Unknown";
	public string Speed { get; set; } = "Unknown";
	public string MemoryType { get; set; } = "Unknown";
	public string FormFactor { get; set; } = "Unknown";
	public string EccType { get; set; } = "Unknown";
	public string DimmType { get; set; } = "Unknown";
}