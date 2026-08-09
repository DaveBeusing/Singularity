// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.Hardware.Utils;

namespace Singularity.Hardware.Providers;

public sealed class StorageProvider
{
	public IReadOnlyList<StorageInventory> Read()
	{
		List<StorageInventory> drives = new();

		List<PhysicalDiskInfo> physicalDisks = ReadPhysicalDisks();

		try
		{
			int index = 0;

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
				string model = WmiConverter.ToString(obj["Model"]);
				string interfaceType = WmiConverter.ToString(obj["InterfaceType"]);
				string mediaType = WmiConverter.ToString(obj["MediaType"]);

				PhysicalDiskInfo physicalDisk =
					index < physicalDisks.Count
						? physicalDisks[index]
						: new PhysicalDiskInfo();

				string manufacturer = NormalizeManufacturer(
					physicalDisk.Manufacturer,
					model);

				string effectiveMediaType =
					physicalDisk.MediaType != "Unknown"
						? physicalDisk.MediaType
						: mediaType;

				string busType =
					physicalDisk.BusType != "Unknown"
						? physicalDisk.BusType
						: interfaceType;

				bool isNvme = DetectNvme(model, interfaceType, effectiveMediaType, busType);
				bool isSsd = DetectSsd(model, effectiveMediaType, isNvme);

				drives.Add(new StorageInventory
				{
					Model = model,
					Manufacturer = manufacturer,
					Size = FormatSize(WmiConverter.ToUInt64(obj["Size"])),
					InterfaceType = interfaceType,
					MediaType = effectiveMediaType,
					BusType = busType,
					SerialNumber = WmiConverter.ToString(obj["SerialNumber"]),
					FirmwareRevision = WmiConverter.ToString(obj["FirmwareRevision"]),
					IsNvme = isNvme,
					IsSsd = isSsd
				});

				index++;
			}
		}
		catch
		{
		}

		return drives;
	}

	private static List<PhysicalDiskInfo> ReadPhysicalDisks()
	{
		List<PhysicalDiskInfo> disks = new();

		try
		{
			foreach (var obj in WmiQuery.Execute(
				@"\\.\root\Microsoft\Windows\Storage",
				@"SELECT
					FriendlyName,
					Manufacturer,
					MediaType,
					BusType,
					SerialNumber
				FROM MSFT_PhysicalDisk"))
			{
				disks.Add(new PhysicalDiskInfo
				{
					FriendlyName = WmiConverter.ToString(obj["FriendlyName"]),
					Manufacturer = WmiConverter.ToString(obj["Manufacturer"]),
					MediaType = DecodeMediaType(WmiConverter.ToUInt16(obj["MediaType"])),
					BusType = DecodeBusType(WmiConverter.ToUInt16(obj["BusType"])),
					SerialNumber = WmiConverter.ToString(obj["SerialNumber"])
				});
			}
		}
		catch
		{
		}

		return disks;
	}

	private static string DecodeMediaType(ushort mediaType)
	{
		return mediaType switch
		{
			3 => "HDD",
			4 => "SSD",
			5 => "SCM",
			_ => "Unknown"
		};
	}

	private static string DecodeBusType(ushort busType)
	{
		return busType switch
		{
			1 => "SCSI",
			2 => "ATAPI",
			3 => "ATA",
			4 => "IEEE 1394",
			5 => "SSA",
			6 => "Fibre Channel",
			7 => "USB",
			8 => "RAID",
			9 => "iSCSI",
			10 => "SAS",
			11 => "SATA",
			12 => "SD",
			13 => "MMC",
			14 => "Virtual",
			15 => "File Backed Virtual",
			16 => "Storage Spaces",
			17 => "NVMe",
			18 => "Microsoft Reserved",
			_ => "Unknown"
		};
	}

	private static bool DetectNvme(string model, string interfaceType, string mediaType, string busType)
	{
		return Contains(model, "NVME")
			|| Contains(model, "NVM")
			|| Contains(interfaceType, "NVME")
			|| Contains(mediaType, "NVME")
			|| Contains(busType, "NVME");
	}

	private static bool DetectSsd(string model, string mediaType, bool isNvme)
	{
		if (isNvme)
			return true;
		return Contains(model, "SSD") || Contains(mediaType, "SSD") || Contains(model, "M.2");
	}

	private static string NormalizeManufacturer(string manufacturer, string model)
	{
		if (!string.IsNullOrWhiteSpace(manufacturer)
			&& manufacturer != "Unknown")
		{
			return manufacturer.Trim();
		}

		string normalizedModel = model.ToUpperInvariant();

		if (normalizedModel.Contains("SAMSUNG"))
			return "Samsung";

		if (normalizedModel.Contains("CRUCIAL") || normalizedModel.StartsWith("CT"))
			return "Crucial";

		if (normalizedModel.Contains("MICRON") || normalizedModel.StartsWith("MT"))
			return "Micron";

		if (normalizedModel.Contains("KINGSTON"))
			return "Kingston";

		if (normalizedModel.Contains("WESTERN DIGITAL") || normalizedModel.Contains(" WDC ") || normalizedModel.StartsWith("WDC"))
			return "Western Digital";

		if (normalizedModel.Contains("SEAGATE") || normalizedModel.Contains("ST"))
			return "Seagate";

		if (normalizedModel.Contains("INTEL"))
			return "Intel";

		if (normalizedModel.Contains("KIOXIA") || normalizedModel.Contains("TOSHIBA"))
			return "Kioxia";

		return "Unknown";
	}

	private static bool Contains(string value, string token)
	{
		return value.Contains(
			token,
			StringComparison.OrdinalIgnoreCase);
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

	private sealed class PhysicalDiskInfo
	{
		public string FriendlyName { get; init; } = "Unknown";
		public string Manufacturer { get; init; } = "Unknown";
		public string MediaType { get; init; } = "Unknown";
		public string BusType { get; init; } = "Unknown";
		public string SerialNumber { get; init; } = "Unknown";
	}

}