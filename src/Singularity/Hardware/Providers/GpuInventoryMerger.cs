// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;

namespace Singularity.Hardware.Providers;

public static class GpuInventoryMerger
{
	public static IReadOnlyList<GpuInventory> Merge(
		IReadOnlyList<GpuInventory> baseline,
		IReadOnlyList<GpuInventory> enrichment)
	{
		ArgumentNullException.ThrowIfNull(baseline);
		ArgumentNullException.ThrowIfNull(enrichment);

		if (baseline.Count == 0)
		{
			return enrichment
				.Where(gpu => !GpuDeviceIdentity.IsTransientNvmlIdentifier(gpu.Identifier))
				.Select(Clone)
				.ToArray();
		}

		List<GpuInventory> result = new(baseline.Count);
		HashSet<int> consumedEnrichment = [];

		foreach (GpuInventory baselineGpu in baseline)
		{
			int matchIndex = FindMatch(baselineGpu, enrichment, consumedEnrichment);
			if (matchIndex < 0)
			{
				result.Add(Clone(baselineGpu));
				continue;
			}

			consumedEnrichment.Add(matchIndex);
			result.Add(MergeDevice(baselineGpu, enrichment[matchIndex]));
		}

		return result.AsReadOnly();
	}

	private static int FindMatch(
		GpuInventory baseline,
		IReadOnlyList<GpuInventory> enrichment,
		IReadOnlySet<int> consumed)
	{
		for (int index = 0; index < enrichment.Count; index++)
		{
			if (consumed.Contains(index))
				continue;

			GpuInventory candidate = enrichment[index];
			if (GpuDeviceIdentity.IsTransientNvmlIdentifier(candidate.Identifier))
				continue;

			if (baseline.AdapterLuid.HasValue &&
				candidate.AdapterLuid.HasValue &&
				baseline.AdapterLuid.Value == candidate.AdapterLuid.Value)
			{
				return index;
			}

			if (!string.IsNullOrWhiteSpace(baseline.Identifier) &&
				string.Equals(
					baseline.Identifier,
					candidate.Identifier,
					StringComparison.OrdinalIgnoreCase))
			{
				return index;
			}
		}

		return -1;
	}

	private static GpuInventory MergeDevice(
		GpuInventory baseline,
		GpuInventory enrichment)
	{
		return new GpuInventory
		{
			Identifier = IsAvailableIdentifier(enrichment.Identifier)
				? enrichment.Identifier
				: baseline.Identifier,
			AdapterLuid = baseline.AdapterLuid ?? enrichment.AdapterLuid,
			AdapterIndex = baseline.AdapterIndex,
			Vendor = Prefer(enrichment.Vendor, baseline.Vendor),
			VendorId = baseline.VendorId ?? enrichment.VendorId,
			DeviceId = baseline.DeviceId ?? enrichment.DeviceId,
			SubsystemId = baseline.SubsystemId ?? enrichment.SubsystemId,
			Revision = baseline.Revision ?? enrichment.Revision,
			DedicatedVideoMemoryBytes =
				enrichment.DedicatedVideoMemoryBytes ??
				baseline.DedicatedVideoMemoryBytes,
			IsNvidia = baseline.IsNvidia || enrichment.IsNvidia,
			IsDirect3D12Capable = baseline.IsDirect3D12Capable,
			Name = Prefer(enrichment.Name, baseline.Name),
			Vram = Prefer(enrichment.Vram, baseline.Vram),
			Temperature = Prefer(enrichment.Temperature, baseline.Temperature),
			PcieGenerationCurrent = Prefer(
				enrichment.PcieGenerationCurrent,
				baseline.PcieGenerationCurrent),
			PcieGenerationMax = Prefer(
				enrichment.PcieGenerationMax,
				baseline.PcieGenerationMax),
			PcieWidthCurrent = Prefer(
				enrichment.PcieWidthCurrent,
				baseline.PcieWidthCurrent),
			PcieWidthMax = Prefer(
				enrichment.PcieWidthMax,
				baseline.PcieWidthMax),
			Details = Prefer(enrichment.Details, baseline.Details)
		};
	}

	private static GpuInventory Clone(GpuInventory source)
	{
		return new GpuInventory
		{
			Identifier = source.Identifier,
			AdapterLuid = source.AdapterLuid,
			AdapterIndex = source.AdapterIndex,
			Vendor = source.Vendor,
			VendorId = source.VendorId,
			DeviceId = source.DeviceId,
			SubsystemId = source.SubsystemId,
			Revision = source.Revision,
			DedicatedVideoMemoryBytes = source.DedicatedVideoMemoryBytes,
			IsNvidia = source.IsNvidia,
			IsDirect3D12Capable = source.IsDirect3D12Capable,
			Name = source.Name,
			Vram = source.Vram,
			Temperature = source.Temperature,
			PcieGenerationCurrent = source.PcieGenerationCurrent,
			PcieGenerationMax = source.PcieGenerationMax,
			PcieWidthCurrent = source.PcieWidthCurrent,
			PcieWidthMax = source.PcieWidthMax,
			Details = source.Details
		};
	}

	private static bool IsAvailableIdentifier(string? value) =>
		!string.IsNullOrWhiteSpace(value) &&
		!GpuDeviceIdentity.IsTransientNvmlIdentifier(value);

	private static string Prefer(string? preferred, string? fallback)
	{
		return IsAvailableValue(preferred)
			? preferred!
			: IsAvailableValue(fallback)
				? fallback!
				: "Unavailable";
	}

	private static bool IsAvailableValue(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return false;

		return !string.Equals(value, "Unknown", StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(value, "Unavailable", StringComparison.OrdinalIgnoreCase) &&
			!value.EndsWith(" Unknown", StringComparison.OrdinalIgnoreCase);
	}
}
