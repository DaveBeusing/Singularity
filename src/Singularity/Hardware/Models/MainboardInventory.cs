// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Models;

public sealed class MainboardInventory
{
	public string Manufacturer { get; init; } = "Unknown";
	public string Product { get; init; } = "Unknown";
	public string BiosVersion { get; init; } = "Unknown";
	public string BiosDate { get; init; } = "Unknown";
	public string Name => $"{Manufacturer} {Product}".Trim();
	public string Details => $"BIOS {BiosVersion} ({BiosDate})";

}