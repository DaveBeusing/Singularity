// Copyright (c) 2026 David Beusing
// Licensed under the MIT License.
using System.Runtime.InteropServices;

namespace Singularity.Monitoring;

/// <summary>
/// Liest NVIDIA GPU-Informationen direkt über NVML.
/// NVML wird mit dem NVIDIA-Treiber installiert.
/// Es wird keine NuGet-Bibliothek verwendet.
/// </summary>
public sealed class NvmlGpuInfoReader
{
	public GpuInfo ReadPrimaryGpu()
	{
		try
		{
			NvmlReturn result = NvmlInit();
			if (result != NvmlReturn.Success)
			{
				return new GpuInfo
				{
					Name = "NVIDIA GPU",
					Details = "NVML initialization failed"
				};
			}
			try
			{
				result = NvmlDeviceGetHandleByIndex(0, out IntPtr device);
				if (result != NvmlReturn.Success)
				{
					return new GpuInfo
					{
						Name = "NVIDIA GPU",
						Details = "NVML device not found"
					};
				}
				string name = ReadGpuName(device);
				string driver = ReadDriverVersion();
				string memory = ReadMemoryInfo(device);
				string temperature = ReadTemperature(device);

				NvmlDeviceGetCurrPcieLinkGeneration(device, out uint currentGeneration);
				NvmlDeviceGetMaxPcieLinkGeneration(device, out uint maxGeneration);
				NvmlDeviceGetCurrPcieLinkWidth(device, out uint currentWidth);
				NvmlDeviceGetMaxPcieLinkWidth(device, out uint maxWidth);

				return new GpuInfo
				{
					Name = name,
					Vram = memory,
					Temperature = temperature,
					PcieGenerationCurrent = currentGeneration.ToString(),
					PcieGenerationMax = maxGeneration.ToString(),
					PcieWidthCurrent = currentWidth.ToString(),
					PcieWidthMax = maxWidth.ToString(),
					Details =
						$"{memory} | " +
						$"{temperature} | " +
						$"PCIe Gen{currentGeneration}x{currentWidth} | " +
						$"[Gen{maxGeneration}x{maxWidth}]"
				};
			}
			finally
			{
				NvmlShutdown();
			}
		}
		catch (DllNotFoundException)
		{
			return new GpuInfo
			{
				Name = "NVIDIA GPU",
				Details = "NVML not found"
			};
		}
		catch
		{
			return new GpuInfo
			{
				Name = "NVIDIA GPU",
				Details = "NVML read failed"
			};
		}
	}

	private static string ReadGpuName(IntPtr device)
	{
		byte[] buffer = new byte[96];
		NvmlReturn result = NvmlDeviceGetName(device, buffer, (uint)buffer.Length);
		if (result != NvmlReturn.Success)
			return "Unknown NVIDIA GPU";
		return DecodeAscii(buffer);
	}

	private static string ReadDriverVersion()
	{
		byte[] buffer = new byte[80];
		NvmlReturn result = NvmlSystemGetDriverVersion(buffer, (uint)buffer.Length);
		if (result != NvmlReturn.Success)
			return "Unknown";
		return DecodeAscii(buffer);
	}

	private static string ReadMemoryInfo(IntPtr device)
	{
		NvmlReturn result = NvmlDeviceGetMemoryInfo(device, out NvmlMemory memory);
		if (result != NvmlReturn.Success)
			return "VRAM Unknown";
		return $"VRAM {FormatBytes(memory.Total)}";
	}

	private static string ReadTemperature(IntPtr device)
	{
		NvmlReturn result = NvmlDeviceGetTemperature(device, NvmlTemperatureSensors.Gpu, out uint temperature);
		if (result != NvmlReturn.Success)
			return "Temp Unknown";
		return $"Temp {temperature} °C";
	}

	private static string FormatBytes(ulong bytes)
	{
		double gb = bytes / 1024d / 1024d / 1024d;
		return $"{gb:0.#}GB";
	}

	private static string DecodeAscii(byte[] buffer)
	{
		int length = Array.IndexOf(buffer, (byte)0);
		if (length < 0)
			length = buffer.Length;
		return System.Text.Encoding.ASCII
			.GetString(buffer, 0, length)
			.Trim();
	}

	private enum NvmlReturn
	{
		Success = 0
	}

	private enum NvmlTemperatureSensors
	{
		Gpu = 0
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct NvmlMemory
	{
		public ulong Total;
		public ulong Free;
		public ulong Used;
	}

	[DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
	private static extern NvmlReturn NvmlInit();

	[DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
	private static extern NvmlReturn NvmlShutdown();

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
	private static extern NvmlReturn NvmlDeviceGetHandleByIndex(uint index, out IntPtr device);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetName")]
	private static extern NvmlReturn NvmlDeviceGetName(IntPtr device, byte[] name, uint length);

	[DllImport("nvml.dll", EntryPoint = "nvmlSystemGetDriverVersion")]
	private static extern NvmlReturn NvmlSystemGetDriverVersion(byte[] version, uint length);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMemoryInfo")]
	private static extern NvmlReturn NvmlDeviceGetMemoryInfo( IntPtr device, out NvmlMemory memory);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
	private static extern NvmlReturn NvmlDeviceGetTemperature(IntPtr device, NvmlTemperatureSensors sensorType, out uint temperature);

	 [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCurrPcieLinkGeneration")]
	private static extern NvmlReturn NvmlDeviceGetCurrPcieLinkGeneration(IntPtr device, out uint generation);
	
	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMaxPcieLinkGeneration")]
	private static extern NvmlReturn NvmlDeviceGetMaxPcieLinkGeneration(IntPtr device, out uint generation);
	
	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCurrPcieLinkWidth")]
	private static extern NvmlReturn NvmlDeviceGetCurrPcieLinkWidth(IntPtr device, out uint width);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMaxPcieLinkWidth")]
	private static extern NvmlReturn NvmlDeviceGetMaxPcieLinkWidth(IntPtr device, out uint width);

}