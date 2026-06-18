// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.

using Singularity.Hardware.Models;
using Singularity.Hardware.Utils;

namespace Singularity.Hardware.Providers;

public sealed class StorageProvider
{
	public IReadOnlyList<StorageInventory> Read()
	{
		List<StorageInventory> drives = new();

		try
		{
			foreach (var obj in WmiQuery.Execute(
				@"SELECT
					Model,
					Size,
					InterfaceType,
					MediaType,
					SerialNumber,
					FirmwareRevision
				FROM Win32_DiskDrive"))
			{
				drives.Add(new StorageInventory
				{
					Model = WmiConverter.ToString(obj["Model"]),
					Size = FormatSize(WmiConverter.ToUInt64(obj["Size"])),
					InterfaceType = WmiConverter.ToString(obj["InterfaceType"]),
					MediaType = WmiConverter.ToString(obj["MediaType"]),
					SerialNumber = WmiConverter.ToString(obj["SerialNumber"]),
					FirmwareRevision = WmiConverter.ToString(obj["FirmwareRevision"])
				});
			}
		}
		catch
		{
		}

		return drives;
	}

	private static string FormatSize(ulong bytes)
	{
		if (bytes == 0)
			return "Unknown";

		double gb = bytes / 1000d / 1000d / 1000d;

		if (gb >= 1000)
			return $"{gb / 1000d:0.##} TB";

		return $"{gb:0} GB";
	}

}