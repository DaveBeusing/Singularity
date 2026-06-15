// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Decoders;
using Singularity.Hardware.Models;
using Singularity.Hardware.Utils;

namespace Singularity.Hardware.Providers;

public sealed class MemoryProvider
{
	public IReadOnlyList<MemoryInventory> Read()
	{
		List<MemoryInventory> modules = new();
		try
		{
			 foreach (var obj in WmiQuery.Execute(
				@"SELECT
					DeviceLocator,
					Capacity,
					Speed,
					Manufacturer,
					PartNumber,
					MemoryType,
					SMBIOSMemoryType,
					FormFactor,
					TotalWidth,
					DataWidth
					FROM Win32_PhysicalMemory"))
			{
				ushort memoryType = WmiConverter.ToUInt16(obj["MemoryType"]);
				ushort smbiosMemoryType = WmiConverter.ToUInt16(obj["SMBIOSMemoryType"]);
				ushort formFactor = WmiConverter.ToUInt16(obj["FormFactor"]);
				ushort totalWidth = WmiConverter.ToUInt16(obj["TotalWidth"]);
				ushort dataWidth = WmiConverter.ToUInt16(obj["DataWidth"]);
				modules.Add(
					new MemoryInventory
					{
						Slot = WmiConverter.ToString(obj["DeviceLocator"]),
						Capacity = FormatCapacity(WmiConverter.ToUInt64(obj["Capacity"])),
						Speed = $"{WmiConverter.ToUInt32(obj["Speed"])} MT/s",
						Manufacturer = JedecManufacturerDecoder.Decode(WmiConverter.ToString(obj["Manufacturer"])),
						PartNumber =  WmiConverter.ToString(obj["PartNumber"]),
						MemoryType = MemoryTypeDecoder.Decode(memoryType, smbiosMemoryType),
						DimmType = DimmTypeDecoder.Decode(formFactor),
						EccType = EccDecoder.Decode(totalWidth, dataWidth)
					});
			}
		}
		catch
		{
		}
		return modules;
	}

	private static string FormatCapacity(ulong bytes)
	{
		if (bytes == 0)
			return "Unknown";
		double gb = bytes / 1024d / 1024d / 1024d;
		return $"{gb:0} GB";
	}

}