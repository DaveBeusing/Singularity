// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Management;
using System.Runtime.InteropServices;

namespace Singularity.Monitoring;

public sealed class HardwareInfoReader
{
	private readonly NvmlGpuInfoReader nvmlGpuInfoReader = new();
	public HardwareInfo Read()
	{
		CpuInfo cpuInfo = ReadCpuInfo();
		//GpuInfo gpuInfo = ReadGpuInfo();
		GpuInfo gpuInfo = nvmlGpuInfoReader.ReadPrimaryGpu();
		return new HardwareInfo
		{
			Mainboard = ReadMainboard(), 
			MainboardDetails = ReadMainboardDetails(),
			Cpu = cpuInfo.Name,
			CpuDetails = cpuInfo.Details,
			Gpu = gpuInfo.Name,
			GpuVram = gpuInfo.Vram,
			GpuTemperature = gpuInfo.Temperature,
			GpuPcieCurrent = $"Gen{gpuInfo.PcieGenerationCurrent}x{gpuInfo.PcieWidthCurrent}",
			GpuPcieMax = $"Gen{gpuInfo.PcieGenerationMax}x{gpuInfo.PcieWidthMax}",
			MemoryModules = ReadMemoryModules()
		};
	}

	private static string ReadMainboard()
	{
		string manufacturer = ReadWmiValue("Win32_BaseBoard", "Manufacturer");
		string product = ReadWmiValue("Win32_BaseBoard", "Product");
		return $"{manufacturer} {product}".Trim();
	}

	//via WMI können wir nicht zuverlässig die PCIe Generation auslesen
	//wir müssten anhand des Product das Chipset auslesen und ggü. PCIe Gen mappen
	//das lassen wir uns holen uns später uas NVML die Anbindung der GPU
	private static string ReadMainboardDetails()
	{
		string boardVersion = ReadWmiValue("Win32_BaseBoard", "Version");
		string boardSerial = ReadWmiValue("Win32_BaseBoard", "SerialNumber");
		string biosManufacturer = ReadWmiValue("Win32_BIOS", "Manufacturer");
		string biosVersion = ReadWmiValue("Win32_BIOS", "SMBIOSBIOSVersion");
		string biosDateRaw = ReadWmiValue("Win32_BIOS", "ReleaseDate");
		string biosDate = FormatWmiDate(biosDateRaw);
		return $"BIOS: {biosVersion} ({biosDate})";//{boardVersion} {boardSerial} {biosManufacturer}
	}

	private static string ReadCpu()
	{
		return ReadWmiValue("Win32_Processor", "Name");
	}

	private static CpuInfo ReadCpuInfo()
	{
		try
		{
			using ManagementObjectSearcher searcher = new(
				@"SELECT
					Name,
					NumberOfCores,
					NumberOfLogicalProcessors,
					MaxClockSpeed,
					L2CacheSize,
					L3CacheSize,
					SocketDesignation,
					VirtualizationFirmwareEnabled
				FROM Win32_Processor");
			foreach (ManagementObject obj in searcher.Get())
			{
				string name = CleanWmiValue(obj["Name"]?.ToString() ?? "Unknown");
				uint cores = ConvertToUInt32(obj["NumberOfCores"]);
				uint threads = ConvertToUInt32(obj["NumberOfLogicalProcessors"]);
				uint maxClockMhz = ConvertToUInt32(obj["MaxClockSpeed"]);
				uint l2CacheKb = ConvertToUInt32(obj["L2CacheSize"]);
				uint l3CacheKb = ConvertToUInt32(obj["L3CacheSize"]);
				string socket = CleanWmiValue(obj["SocketDesignation"]?.ToString() ?? "Unknown");
				bool virtualization = ConvertToBool(obj["VirtualizationFirmwareEnabled"]);
				string architecture = RuntimeInformation.ProcessArchitecture.ToString();

				return new CpuInfo
				{
					Name = name,
					CoreThreadInfo = $"{cores} Cores / {threads} Threads",
					ClockInfo = maxClockMhz > 0 ? $"{maxClockMhz / 1000d:0.00} GHz Max" : "Clock Unknown",
					CacheInfo = $"L2 {FormatKilobytes(l2CacheKb)} / L3 {FormatKilobytes(l3CacheKb)}",
					PlatformInfo = $"{socket} | {architecture} | VT {(virtualization ? "Enabled" : "Disabled")}"
				};
			}
		}
		catch
		{
		}
		return new CpuInfo();
	}

	private static string ReadGpu()
	{
		return ReadWmiValue("Win32_VideoController", "Name");
	}

	private static GpuInfo ReadGpuInfo()
	{
		try
		{
			using ManagementObjectSearcher searcher = new("SELECT Name, AdapterRAM FROM Win32_VideoController");
			foreach (ManagementObject obj in searcher.Get())
			{
				string name = obj["Name"]?.ToString()?.Trim() ?? "Unknown";
				ulong adapterRam = Convert.ToUInt64(obj["AdapterRAM"] ?? 0);
				string vram = FormatBytes(adapterRam);
				return new GpuInfo
				{
					Name = name,
					Details = $"VRAM {vram}"
				};
			}
		}
		catch
		{
		}
		return new GpuInfo();
	}

	private static List<MemoryModuleInfo> ReadMemoryModules()
	{
		List<MemoryModuleInfo> modules = new();
		try
		{
			using ManagementObjectSearcher searcher = new(
				@"SELECT
					BankLabel,
					DeviceLocator,
					Manufacturer,
					PartNumber,
					Capacity,
					Speed,
					SMBIOSMemoryType,
					FormFactor,
					TotalWidth,
					DataWidth
				FROM Win32_PhysicalMemory");
			foreach (ManagementObject obj in searcher.Get())
			{
				ushort memoryType = Convert.ToUInt16(obj["SMBIOSMemoryType"] ?? 0);
				ushort dataWidth = Convert.ToUInt16(obj["DataWidth"] ?? 0);
				ushort totalWidth = Convert.ToUInt16(obj["TotalWidth"] ?? 0);
				ushort formFactor = Convert.ToUInt16(obj["FormFactor"] ?? 0);
				string partNumber = obj["PartNumber"]?.ToString()?.Trim() ?? "Unknown";
				ulong bytes = Convert.ToUInt64(obj["Capacity"]);
				double gb = bytes / 1024d / 1024d / 1024d;
				modules.Add(new MemoryModuleInfo
				{
					Slot = $"{obj["BankLabel"]} {obj["DeviceLocator"]}".Trim(),
					Manufacturer = DecodeMemoryManufacturer(obj["Manufacturer"]?.ToString() ?? "Unknown"),
					PartNumber = obj["PartNumber"]?.ToString()?.Trim() ?? "Unknown",
					Capacity = $"{Math.Round(gb)}GB",
					Speed = $"{obj["Speed"]}MT/s",
					MemoryType = DecodeMemoryType(memoryType),
					FormFactor = DecodeFormFactor(formFactor),
					EccType = DecodeEccType(dataWidth, totalWidth),
					DimmType = DecodeDimmType(partNumber)
				});
			}
		}
		catch
		{
			modules.Add(new MemoryModuleInfo
			{
				Slot = "Unknown",
				Capacity = "Unknown"
			});
		}

		return modules;
	}

	//map SMBIOSMemoryType to readable name
	private static string DecodeMemoryType(ushort type)
	{
		return type switch
		{
			20 => "DDR",
			21 => "DDR2",
			24 => "DDR3",
			26 => "DDR4",
			34 => "DDR5",
			_ => $"Unknown ({type})"
		};
	}

	//map JEDEC-Manufacturer-ID (hexcode) to readable (04CD->G.Skill)
	//TODO extend mappings
	//TODO move to its own place to make it more maintainable -> Monitoring/JedecManufacturerDecoder.cs
	private static string DecodeMemoryManufacturer(string rawManufacturer)
	{
		string value = rawManufacturer.Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();
		return value switch
		{
			// Samsung
			"CE00" => "Samsung",
			"00CE" => "Samsung",
			"CE00000000000000" => "Samsung",

			// SK hynix
			"80AD" => "SK hynix",
			"AD80" => "SK hynix",

			// Micron
			"2C00" => "Micron",
			"002C" => "Micron",

			// Crucial (Micron Consumer)
			"2C80" => "Crucial",
			"802C" => "Crucial",

			// Kingston
			"9801" => "Kingston",
			"0198" => "Kingston",

			// Corsair
			"9E7F7F" => "Corsair",
			"7F7F9E" => "Corsair",

			// A-DATA
			"04CB" => "A-DATA",
			"CB04" => "A-DATA",

			// G.Skill
			"CD04" => "G.Skill",
			"04CD" => "G.Skill",

			// TeamGroup
			"EF00" => "TeamGroup",
			"00EF" => "TeamGroup",

			// Patriot
			"7F7F7F" => "Patriot",

			// Nanya
			"0B00" => "Nanya",
			"000B" => "Nanya",

			// Winbond
			"DA00" => "Winbond",
			"00DA" => "Winbond",

			// Transcend
			"4F01" => "Transcend",
			"014F" => "Transcend",

			// Apacer
			"7A7A" => "Apacer",

			// Innodisk
			"86F1" => "Innodisk",

			// Smart Modular
			"9401" => "SMART Modular",

			// Super Talent
			"CB7F" => "Super Talent",

			//default einfach den Hexcode zürück geben
			//TODO abfangen und melden damit wir erweitern können
			_ => value
		};
	}

	//ECC hat 8bits mehr ;)
	private static string DecodeEccType(ushort dataWidth, ushort totalWidth)
	{
		if (totalWidth > dataWidth)
		{
			return "ECC";
		}
		return "Non-ECC";
		//ternary??
	}

	//WMI liefert keine Daten dazu, workaround ist die P/N nach angaben zu filtern
	//TODO alternativen?
	private static string DecodeDimmType(string partNumber)
	{
		string pn =	partNumber.ToUpperInvariant().Replace(" ", "");
		if (pn.Contains("LRDIMM"))
			return "LRDIMM";
		if (pn.Contains("RDIMM"))
			return "RDIMM";
		if (pn.Contains("UDIMM"))
			return "UDIMM";
		if (pn.Contains("SODIMM"))
			return "SO-DIMM";
		return "DIMM";
	}

	//Module formfactor
	private static string DecodeFormFactor(ushort formFactor)
	{
		return formFactor switch
		{
			8  => "DIMM",
			9  => "TSOP",
			12 => "SO-DIMM",
			13 => "Micro-DIMM",
			15 => "FB-DIMM",
			16 => "Die",
			_  => $"Unknown ({formFactor})"
		};
	}


	private static string FormatBytes(ulong bytes)
	{
		if (bytes == 0)
			return "Unknown";
		double gb = bytes / 1024d / 1024d / 1024d;
		if (gb >= 1)
			return $"{gb:0.#} GB";
		double mb = bytes / 1024d / 1024d;
		return $"{mb:0} MB";
	}

	private static string FormatWmiDate(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw) || raw.Length < 8)
			return "Unknown";
		string year = raw[..4];
		string month = raw.Substring(4, 2);
		string day = raw.Substring(6, 2);
		return $"{year}-{month}-{day}";
	}

	private static string ReadWmiValue(string className, string propertyName)
	{
		try
		{
			using ManagementObjectSearcher searcher = new($"SELECT {propertyName} FROM {className}");
			foreach (ManagementObject obj in searcher.Get())
			{
				object? value = obj[propertyName];
				if (value != null)
					return value.ToString() ?? "Unknown";
			}
		}
		catch
		{
			return "Unknown";
		}
		return "Unknown";
	}

	private static string CleanWmiValue(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return "Unknown";
		string cleaned = value.Trim();
		string normalized = cleaned.ToUpperInvariant();
		string[] invalidValues =
		{
			"DEFAULT STRING",
			"TO BE FILLED BY O.E.M.",
			"TO BE FILLED BY OEM",
			"SYSTEM SERIAL NUMBER",
			"NONE",
			"NULL",
			"N/A",
			"NA",
			"0",
			"UNKNOWN"
		};
		if (invalidValues.Contains(normalized))
			return "Unknown";
		return cleaned;
	}

	private static uint ConvertToUInt32(object? value)
	{
		if (value == null)
			return 0;
		try
		{
			return Convert.ToUInt32(value);
		}
		catch
		{
			return 0;
		}
	}

	private static bool ConvertToBool(object? value)
	{
		if (value == null)
			return false;
		try
		{
			return Convert.ToBoolean(value);
		}
		catch
		{
			return false;
		}
	}

	private static string FormatKilobytes(uint kilobytes)
	{
		if (kilobytes == 0)
			return "Unknown";
		double megabytes = kilobytes / 1024d;
		if (megabytes >= 1)
			return $"{megabytes:0.#} MB";
		return $"{kilobytes} KB";
	}

}