// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;

namespace Singularity.Application;

public sealed record PlatformProperty(string Name, string Value);

public sealed record PlatformDeviceSelection(
	string Kind,
	string Id,
	string DisplayName,
	IReadOnlyList<PlatformProperty> Properties);

public static class PlatformSelectionMapper
{
	public static PlatformDeviceSelection FromMemory(MemoryInventory memory, int index)
	{
		ArgumentNullException.ThrowIfNull(memory);

		return new PlatformDeviceSelection(
			"Memory",
			$"memory-{index}",
			Display(memory.Slot),
			[
				new("Slot", Display(memory.Slot)),
				new("Capacity", Display(memory.Capacity)),
				new("Speed", Display(memory.Speed)),
				new("Manufacturer", Display(memory.Manufacturer)),
				new("Part number", Display(memory.PartNumber)),
				new("Memory type", Display(memory.MemoryType)),
				new("DIMM type", Display(memory.DimmType)),
				new("ECC", Display(memory.EccType))
			]);
	}

	public static PlatformDeviceSelection FromGpu(GpuInventory gpu, int index)
	{
		ArgumentNullException.ThrowIfNull(gpu);

		string id = string.IsNullOrWhiteSpace(gpu.Identifier)
			? $"gpu-{index}"
			: gpu.Identifier;

		return new PlatformDeviceSelection(
			"GPU",
			id,
			Display(gpu.Name),
			[
				new("Adapter", gpu.AdapterIndex.ToString()),
				new("Vendor", Display(gpu.Vendor)),
				new("Vendor ID", Hex(gpu.VendorId)),
				new("Device ID", Hex(gpu.DeviceId)),
				new("Name", Display(gpu.Name)),
				new("VRAM", Display(gpu.Vram)),
				new("Direct3D 12", gpu.IsDirect3D12Capable ? "Available" : "Unavailable"),
				new("PCIe current", PcieLink(gpu.PcieGenerationCurrent, gpu.PcieWidthCurrent)),
				new("PCIe maximum", PcieLink(gpu.PcieGenerationMax, gpu.PcieWidthMax)),
				new("Adapter LUID", gpu.AdapterLuid.HasValue
					? $"0x{unchecked((ulong)gpu.AdapterLuid.Value):X16}"
					: "Unavailable"),
				new("Details", Display(gpu.Details)),
				new("Identifier", Display(gpu.Identifier))
			]);
	}

	public static PlatformDeviceSelection FromStorage(StorageInventory storage, int index)
	{
		ArgumentNullException.ThrowIfNull(storage);

		return new PlatformDeviceSelection(
			"Storage",
			$"storage-{index}",
			Display(storage.Model),
			[
				new("Model", Display(storage.Model)),
				new("Manufacturer", Display(storage.Manufacturer)),
				new("Capacity", Display(storage.Size)),
				new("Device type", Display(storage.DeviceType)),
				new("Interface", Display(storage.InterfaceType)),
				new("Bus", Display(storage.BusType)),
				new("Serial number", Display(storage.SerialNumber)),
				new("Firmware", Display(storage.FirmwareRevision))
			]);
	}

	private static string PcieLink(string? generation, string? width)
	{
		return IsAvailable(generation) && IsAvailable(width)
			? $"Gen{generation} x{width}"
			: "Unavailable";
	}

	private static string Hex(uint? value) =>
		value.HasValue ? $"0x{value.Value:X4}" : "Unavailable";

	private static string Display(string? value) =>
		IsAvailable(value) ? value! : "Unavailable";

	private static bool IsAvailable(string? value)
	{
		return !string.IsNullOrWhiteSpace(value) &&
			!string.Equals(value, "Unknown", StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(value, "Unavailable", StringComparison.OrdinalIgnoreCase);
	}
}
