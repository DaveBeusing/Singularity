// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.

namespace Singularity.Hardware.Models;

public sealed class StorageInventory
{
	public string Model { get; init; } = "Unknown";
	public string Manufacturer { get; init; } = "Unknown";
	public string Size { get; init; } = "Unknown";
	public string InterfaceType { get; init; } = "Unknown";
	public string MediaType { get; init; } = "Unknown";
	public string BusType { get; init; } = "Unknown";
	public string SerialNumber { get; init; } = "Unknown";
	public string FirmwareRevision { get; init; } = "Unknown";
	public bool IsSsd { get; init; }
	public bool IsNvme { get; init; }
	public string DeviceType => IsNvme ? "NVMe SSD" : IsSsd ? "SATA SSD" : "HDD";
	public string Details => $"{Size} | {DeviceType} | Bus {BusType}";

}