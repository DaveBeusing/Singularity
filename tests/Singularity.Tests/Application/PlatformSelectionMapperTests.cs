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
