using System.Management;

namespace Singularity.Monitoring;

public sealed class HardwareInfoReader
{
	public HardwareInfo Read()
	{
		return new HardwareInfo{Mainboard = ReadMainboard(), Cpu = ReadCpu(), Gpu = ReadGpu()};
	}

	private static string ReadMainboard()
	{
		string manufacturer = ReadWmiValue("Win32_BaseBoard", "Manufacturer");
		string product = ReadWmiValue("Win32_BaseBoard", "Product");
		return $"{manufacturer} {product}".Trim();
	}

	private static string ReadCpu()
	{
		return ReadWmiValue("Win32_Processor", "Name");
	}

	private static string ReadGpu()
	{
		return ReadWmiValue("Win32_VideoController", "Name");
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

}