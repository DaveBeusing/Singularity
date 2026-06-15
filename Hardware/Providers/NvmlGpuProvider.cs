// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Text;
using Singularity.Hardware.Models;
using Singularity.Hardware.Native.Nvml;

namespace Singularity.Hardware.Providers;

public sealed class NvmlGpuProvider
{
	public GpuInventory Read()
	{
		try
		{
			NvmlReturn result = NvmlNative.Init();
			if (result != NvmlReturn.Success)
			{
				return new GpuInventory
				{
					Name = "NVIDIA GPU",
					Details = "NVML initialization failed"
				};
			}
			try
			{
				result = NvmlNative.DeviceGetHandleByIndex(0, out IntPtr device);
				if (result != NvmlReturn.Success)
				{
					return new GpuInventory
					{
						Name = "NVIDIA GPU",
						Details = "NVML device not found"
					};
				}
				string name = ReadGpuName(device);
				//string driver = ReadDriverVersion();
				string memory = ReadMemoryInfo(device);
				string temperature = ReadTemperature(device);

				ReadPcieInfo(device, out string currentGeneration, out string maxGeneration, out string currentWidth, out string maxWidth);

				return new GpuInventory
				{
					Name = name,
					Vram = memory,
					Temperature = temperature,
					PcieGenerationCurrent = currentGeneration,
					PcieGenerationMax = maxGeneration,
					PcieWidthCurrent = currentWidth,
					PcieWidthMax = maxWidth,
					Details =
						$"{memory} | " +
						$"{temperature} | " +
						$"PCIe Gen{currentGeneration}x{currentWidth} | " +
						$"[Gen{maxGeneration}x{maxWidth}]"
				};
			}
			finally
			{
				NvmlNative.Shutdown();
			}
		}
		catch (DllNotFoundException)
		{
			return new GpuInventory
			{
				Name = "NVIDIA GPU",
				Details = "NVML not found"
			};
		}
		catch
		{
			return new GpuInventory
			{
				Name = "NVIDIA GPU",
				Details = "NVML read failed"
			};
		}
	}

	private static string ReadGpuName(IntPtr device)
	{
		byte[] buffer = new byte[96];
		NvmlReturn result = NvmlNative.DeviceGetName(device, buffer, (uint)buffer.Length);
		return result == NvmlReturn.Success ? DecodeAscii(buffer) : "Unknown NVIDIA GPU";
	}

//	private static string ReadDriverVersion()
//	{
//		byte[] buffer = new byte[80];
//		NvmlReturn result = NvmlSystemGetDriverVersion(buffer, (uint)buffer.Length);
//		if (result != NvmlReturn.Success)
//			return "Unknown";
//		return DecodeAscii(buffer);
//	}

	private static string ReadMemoryInfo(IntPtr device)
	{
		NvmlReturn result = NvmlNative.DeviceGetMemoryInfo(device, out NvmlMemory memory);
		return result == NvmlReturn.Success ? $"VRAM {FormatBytes(memory.Total)}" : "VRAM Unknown";
	}

	private static string ReadTemperature(IntPtr device)
	{
		NvmlReturn result = NvmlNative.DeviceGetTemperature(device, NvmlTemperatureSensor.Gpu, out uint temperature);
		return result == NvmlReturn.Success ? $"Temp {temperature} °C" : "Temp Unknown";
	}

	private static void ReadPcieInfo(IntPtr device, out string currentGeneration, out string maxGeneration, out string currentWidth, out string maxWidth)
	{
		currentGeneration = "Unknown";
		maxGeneration = "Unknown";
		currentWidth = "Unknown";
		maxWidth = "Unknown";

		if (NvmlNative.DeviceGetCurrPcieLinkGeneration(device, out uint currGen) == NvmlReturn.Success)
		{
			currentGeneration = currGen.ToString();
		}

		if (NvmlNative.DeviceGetMaxPcieLinkGeneration(device, out uint maxGen) == NvmlReturn.Success)
		{
			maxGeneration = maxGen.ToString();
		}

		if (NvmlNative.DeviceGetCurrPcieLinkWidth(device, out uint currWidth) == NvmlReturn.Success)
		{
			currentWidth = currWidth.ToString();
		}

		if (NvmlNative.DeviceGetMaxPcieLinkWidth(device, out uint maxLinkWidth) == NvmlReturn.Success)
		{
			maxWidth = maxLinkWidth.ToString();
		}
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
		return System.Text.Encoding.ASCII.GetString(buffer, 0, length).Trim();
	}

}