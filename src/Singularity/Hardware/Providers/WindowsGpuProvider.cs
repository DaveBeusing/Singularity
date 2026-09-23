// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using static Vortice.Direct3D12.D3D12;
using static Vortice.DXGI.DXGI;

namespace Singularity.Hardware.Providers;

public sealed class WindowsGpuProvider
{
	private const uint NvidiaVendorId = 0x10DE;
	private const uint AmdVendorId = 0x1002;
	private const uint IntelVendorId = 0x8086;

	public IReadOnlyList<GpuInventory> ReadAll(
		IReadOnlyList<NvidiaGpuAdapterIdentity>? nvidiaIdentities = null)
	{
		try
		{
			nvidiaIdentities ??= NvidiaGpuAdapterIdentityProvider.ReadAll();
			Dictionary<long, string> nvidiaIdentifiers = nvidiaIdentities
				.GroupBy(identity => identity.AdapterLuid)
				.ToDictionary(group => group.Key, group => group.First().Identifier);

			using IDXGIFactory1 factory = CreateDXGIFactory1<IDXGIFactory1>();
			List<GpuInventory> gpus = [];

			for (uint index = 0;
				factory.EnumAdapters1(index, out IDXGIAdapter1? adapter).Success;
				index++)
			{
				if (adapter is null)
					continue;

				using (adapter)
				{
					AdapterDescription1 description = adapter.Description1;
					if ((description.Flags & AdapterFlags.Software) != AdapterFlags.None)
						continue;

					long adapterLuid = description.Luid;
					bool isNvidia = description.VendorId == NvidiaVendorId;
					string identifier = isNvidia &&
						nvidiaIdentifiers.TryGetValue(adapterLuid, out string? nvidiaIdentifier)
							? nvidiaIdentifier
							: GpuDeviceIdentity.FromAdapterLuid(adapterLuid);
					ulong dedicatedMemory = (ulong)description.DedicatedVideoMemory;
					bool direct3D12Capable = SupportsDirect3D12(adapter);
					string vendor = GetVendorName(description.VendorId);
					string vram = FormatBytes(dedicatedMemory);

					gpus.Add(new GpuInventory
					{
						Identifier = identifier,
						AdapterLuid = adapterLuid,
						AdapterIndex = checked((int)index),
						Vendor = vendor,
						VendorId = description.VendorId,
						DeviceId = description.DeviceId,
						SubsystemId = description.SubsystemId,
						Revision = description.Revision,
						DedicatedVideoMemoryBytes = dedicatedMemory,
						IsNvidia = isNvidia,
						IsDirect3D12Capable = direct3D12Capable,
						Name = string.IsNullOrWhiteSpace(description.Description)
							? $"{vendor} GPU"
							: description.Description.Trim(),
						Vram = vram,
						Temperature = "Unavailable",
						PcieGenerationCurrent = "Unavailable",
						PcieGenerationMax = "Unavailable",
						PcieWidthCurrent = "Unavailable",
						PcieWidthMax = "Unavailable",
						Details = $"{vendor} | Device 0x{description.DeviceId:X4} | {vram} | Direct3D 12 {(direct3D12Capable ? "available" : "unavailable")}"
					});
				}
			}

			return gpus.AsReadOnly();
		}
		catch
		{
			return Array.Empty<GpuInventory>();
		}
	}

	public static string GetVendorName(uint vendorId)
	{
		return vendorId switch
		{
			NvidiaVendorId => "NVIDIA",
			AmdVendorId => "AMD",
			IntelVendorId => "Intel",
			_ => $"Vendor 0x{vendorId:X4}"
		};
	}

	private static bool SupportsDirect3D12(IDXGIAdapter1 adapter)
	{
		ID3D12Device? device = null;
		try
		{
			return D3D12CreateDevice(
				adapter,
				FeatureLevel.Level_11_0,
				out device).Success &&
				device is not null;
		}
		catch
		{
			return false;
		}
		finally
		{
			device?.Dispose();
		}
	}

	private static string FormatBytes(ulong bytes)
	{
		double gb = bytes / 1024d / 1024d / 1024d;
		return $"VRAM {gb:0.#}GB";
	}
}
