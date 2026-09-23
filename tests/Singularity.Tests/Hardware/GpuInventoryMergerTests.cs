// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.Hardware.Providers;

namespace Singularity.Tests.Hardware;

public sealed class GpuInventoryMergerTests
{
	[Fact]
	public void BaselineOnlyAdapterIsPreserved()
	{
		GpuInventory baseline = AmdGpu(11, "AMD Radeon");

		GpuInventory merged = Assert.Single(
			GpuInventoryMerger.Merge([baseline], Array.Empty<GpuInventory>()));

		Assert.Equal(baseline.Identifier, merged.Identifier);
		Assert.Equal("AMD", merged.Vendor);
		Assert.False(merged.IsNvidia);
		Assert.Equal("Unavailable", merged.Temperature);
	}

	[Fact]
	public void MatchingNvmlDeviceEnrichesBaselineWithoutDuplicate()
	{
		GpuInventory baseline = NvidiaGpu(
			42,
			GpuDeviceIdentity.FromAdapterLuid(42),
			"Windows NVIDIA Adapter");
		GpuInventory enrichment = NvidiaGpu(
			42,
			"GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
			"NVIDIA RTX Example");
		enrichment.Temperature = "Temp 58 °C";
		enrichment.PcieGenerationCurrent = "5";
		enrichment.PcieWidthCurrent = "16";

		GpuInventory merged = Assert.Single(
			GpuInventoryMerger.Merge([baseline], [enrichment]));

		Assert.Equal(enrichment.Identifier, merged.Identifier);
		Assert.Equal(42, merged.AdapterLuid);
		Assert.Equal("NVIDIA RTX Example", merged.Name);
		Assert.Equal("Temp 58 °C", merged.Temperature);
		Assert.Equal("5", merged.PcieGenerationCurrent);
		Assert.Equal("16", merged.PcieWidthCurrent);
	}

	[Fact]
	public void MultipleAdaptersRetainBaselineOrderAndIdentity()
	{
		GpuInventory amd = AmdGpu(7, "AMD Radeon");
		GpuInventory intel = IntelGpu(8, "Intel Graphics");
		GpuInventory nvidia = NvidiaGpu(
			9,
			"GPU-11111111-2222-3333-4444-555555555555",
			"NVIDIA RTX");

		IReadOnlyList<GpuInventory> merged =
			GpuInventoryMerger.Merge([amd, intel, nvidia], [nvidia]);

		Assert.Equal(3, merged.Count);
		Assert.Equal([amd.Identifier, intel.Identifier, nvidia.Identifier],
			merged.Select(gpu => gpu.Identifier));
	}

	[Fact]
	public void UnavailableEnrichmentDoesNotOverwriteBaselineValues()
	{
		GpuInventory baseline = NvidiaGpu(
			15,
			"GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
			"NVIDIA Baseline");
		baseline.Vram = "VRAM 24GB";

		GpuInventory enrichment = NvidiaGpu(
			15,
			baseline.Identifier,
			"Unknown");
		enrichment.Vram = "VRAM Unknown";
		enrichment.Temperature = "Temp Unknown";

		GpuInventory merged = Assert.Single(
			GpuInventoryMerger.Merge([baseline], [enrichment]));

		Assert.Equal("NVIDIA Baseline", merged.Name);
		Assert.Equal("VRAM 24GB", merged.Vram);
		Assert.Equal("Unavailable", merged.Temperature);
	}

	[Fact]
	public void UnmatchedEnrichmentIsNotAppendedToBaselineInventory()
	{
		GpuInventory amd = AmdGpu(21, "AMD Radeon");
		GpuInventory unrelatedNvml = NvidiaGpu(
			22,
			"GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
			"NVIDIA RTX");

		GpuInventory merged = Assert.Single(
			GpuInventoryMerger.Merge([amd], [unrelatedNvml]));

		Assert.Equal(amd.Identifier, merged.Identifier);
	}

	[Fact]
	public void StableNvmlInventoryCanBeUsedWhenWindowsBaselineIsUnavailable()
	{
		GpuInventory stable = NvidiaGpu(
			31,
			"GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
			"NVIDIA RTX");
		GpuInventory transient = NvidiaGpu(
			32,
			"nvml:32",
			"NVIDIA Transient");

		GpuInventory merged = Assert.Single(
			GpuInventoryMerger.Merge(
				Array.Empty<GpuInventory>(),
				[stable, transient]));

		Assert.Equal(stable.Identifier, merged.Identifier);
	}

	private static GpuInventory AmdGpu(long luid, string name) =>
		new()
		{
			Identifier = GpuDeviceIdentity.FromAdapterLuid(luid),
			AdapterLuid = luid,
			Vendor = "AMD",
			VendorId = 0x1002,
			DeviceId = 0x1234,
			IsNvidia = false,
			IsDirect3D12Capable = true,
			Name = name,
			Vram = "VRAM 16GB",
			Temperature = "Unavailable",
			PcieGenerationCurrent = "Unavailable",
			PcieGenerationMax = "Unavailable",
			PcieWidthCurrent = "Unavailable",
			PcieWidthMax = "Unavailable"
		};

	private static GpuInventory IntelGpu(long luid, string name) =>
		new()
		{
			Identifier = GpuDeviceIdentity.FromAdapterLuid(luid),
			AdapterLuid = luid,
			Vendor = "Intel",
			VendorId = 0x8086,
			IsNvidia = false,
			IsDirect3D12Capable = true,
			Name = name,
			Vram = "VRAM 1GB",
			Temperature = "Unavailable"
		};

	private static GpuInventory NvidiaGpu(
		long luid,
		string identifier,
		string name) =>
		new()
		{
			Identifier = identifier,
			AdapterLuid = luid,
			Vendor = "NVIDIA",
			VendorId = 0x10DE,
			IsNvidia = true,
			IsDirect3D12Capable = true,
			Name = name,
			Vram = "VRAM 24GB",
			Temperature = "Unavailable",
			PcieGenerationCurrent = "Unavailable",
			PcieGenerationMax = "Unavailable",
			PcieWidthCurrent = "Unavailable",
			PcieWidthMax = "Unavailable"
		};
}
