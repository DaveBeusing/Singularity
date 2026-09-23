// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Hardware.Models;
using Xunit;

namespace Singularity.Tests.Application;

public sealed class PlatformSelectionMapperTests
{
	[Fact]
	public void MemorySelectionContainsInspectableMetadata()
	{
		MemoryInventory memory = new()
		{
			Slot = "DIMM_A1",
			Capacity = "32 GB",
			Speed = "5600 MT/s",
			Manufacturer = "Example",
			PartNumber = "MEM-32",
			MemoryType = "DDR5",
			DimmType = "UDIMM",
			EccType = "ECC"
		};

		PlatformDeviceSelection selection = PlatformSelectionMapper.FromMemory(memory, 0);

		Assert.Equal("Memory", selection.Kind);
		Assert.Equal("memory-0", selection.Id);
		Assert.Contains(selection.Properties, property => property.Name == "Part number" && property.Value == "MEM-32");
		Assert.Contains(selection.Properties, property => property.Name == "ECC" && property.Value == "ECC");
	}

	[Fact]
	public void GpuSelectionUsesStableProviderIdentifierWhenAvailable()
	{
		GpuInventory gpu = new()
		{
			Identifier = "gpu-uuid",
			AdapterIndex = 2,
			Name = "Example GPU",
			Vram = "24 GB",
			PcieGenerationCurrent = "4",
			PcieGenerationMax = "5",
			PcieWidthCurrent = "16",
			PcieWidthMax = "16"
		};

		PlatformDeviceSelection selection = PlatformSelectionMapper.FromGpu(gpu, 3);

		Assert.Equal("gpu-uuid", selection.Id);
		Assert.Equal("Example GPU", selection.DisplayName);
		Assert.Contains(selection.Properties, property => property.Name == "Adapter" && property.Value == "2");
		Assert.Contains(selection.Properties, property => property.Name == "Direct3D 12" && property.Value == "Available");
	}

	[Fact]
	public void GpuSelectionRepresentsVendorNeutralMetadataAndUnavailablePcie()
	{
		GpuInventory gpu = new()
		{
			Identifier = "dxgi:luid:0000000000000042",
			AdapterLuid = 0x42,
			AdapterIndex = 1,
			Vendor = "AMD",
			VendorId = 0x1002,
			DeviceId = 0x744C,
			IsNvidia = false,
			IsDirect3D12Capable = true,
			Name = "AMD Radeon",
			PcieGenerationCurrent = "Unavailable",
			PcieWidthCurrent = "Unavailable"
		};

		PlatformDeviceSelection selection = PlatformSelectionMapper.FromGpu(gpu, 1);

		Assert.Equal(gpu.Identifier, selection.Id);
		Assert.Contains(selection.Properties, property => property.Name == "Vendor" && property.Value == "AMD");
		Assert.Contains(selection.Properties, property => property.Name == "Vendor ID" && property.Value == "0x1002");
		Assert.Contains(selection.Properties, property => property.Name == "Device ID" && property.Value == "0x744C");
		Assert.Contains(selection.Properties, property => property.Name == "PCIe current" && property.Value == "Unavailable");
		Assert.Contains(selection.Properties, property => property.Name == "Adapter LUID" && property.Value == "0x0000000000000042");
	}

	[Fact]
	public void StorageSelectionMapsMissingValuesToUnavailable()
	{
		StorageInventory storage = new()
		{
			Model = "Example SSD",
			SerialNumber = ""
		};

		PlatformDeviceSelection selection = PlatformSelectionMapper.FromStorage(storage, 1);

		Assert.Equal("storage-1", selection.Id);
		Assert.Contains(selection.Properties, property => property.Name == "Serial number" && property.Value == "Unavailable");
	}
}
