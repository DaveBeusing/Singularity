// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Models;

public sealed class MemoryInventory
{
	public string Slot { get; init; } = "Unknown";
	public string Capacity { get; init; } = "Unknown";
	public string Speed { get; init; } = "Unknown";
	public string Manufacturer { get; init; } = "Unknown";
	public string PartNumber { get; init; } = "Unknown";
	public string MemoryType { get; init; } = "Unknown";
	public string DimmType { get; init; } = "Unknown";
	public string EccType { get; init; } = "Unknown";

}