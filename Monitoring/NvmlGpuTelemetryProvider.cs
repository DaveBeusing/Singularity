// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Native.Nvml;

namespace Singularity.Monitoring;

public sealed class NvmlGpuTelemetryProvider
{
	public GpuTelemetrySnapshot Read()
	{
		try
		{
			NvmlReturn result = NvmlNative.Init();

			if (result != NvmlReturn.Success)
			{
				return new GpuTelemetrySnapshot
				{
					IsAvailable = false,
					Status = "NVML init failed"
				};
			}

			try
			{
				result = NvmlNative.DeviceGetHandleByIndex(0, out IntPtr device);

				if (result != NvmlReturn.Success)
				{
					return new GpuTelemetrySnapshot
					{
						IsAvailable = false,
						Status = "GPU not found"
					};
				}

				NvmlUtilization utilization = ReadUtilization(device);
				NvmlMemory memory = ReadMemory(device);
				int temperature = ReadTemperature(device);

				ReadPower(device, out bool powerAvailable, out double powerWatts);

				return new GpuTelemetrySnapshot
				{
					IsAvailable = true,

					LoadPercent = utilization.Gpu,
					MemoryControllerLoadPercent = utilization.Memory,

					MemoryTotalBytes = memory.Total,
					MemoryUsedBytes = memory.Used,
					MemoryFreeBytes = memory.Free,

					TemperatureCelsius = temperature,

					PowerAvailable = powerAvailable,
					PowerWatts = powerWatts,

					Status = "OK"
				};
			}
			finally
			{
				NvmlNative.Shutdown();
			}
		}
		catch (DllNotFoundException)
		{
			return new GpuTelemetrySnapshot
			{
				IsAvailable = false,
				Status = "NVML not found"
			};
		}
		catch
		{
			return new GpuTelemetrySnapshot
			{
				IsAvailable = false,
				Status = "NVML read failed"
			};
		}
	}

	private static NvmlUtilization ReadUtilization(IntPtr device)
	{
		NvmlReturn result = NvmlNative.DeviceGetUtilizationRates(device, out NvmlUtilization utilization);
		return result == NvmlReturn.Success ? utilization : new NvmlUtilization();
	}

	private static NvmlMemory ReadMemory(IntPtr device)
	{
		NvmlReturn result = NvmlNative.DeviceGetMemoryInfo(device, out NvmlMemory memory);
		return result == NvmlReturn.Success ? memory : new NvmlMemory();
	}

	private static int ReadTemperature(IntPtr device)
	{
		NvmlReturn result = NvmlNative.DeviceGetTemperature(device, NvmlTemperatureSensor.Gpu, out uint temperature);
		return result == NvmlReturn.Success ? (int)temperature : 0;
	}

	private static void ReadPower(IntPtr device, out bool available, out double watts)
	{
		NvmlReturn result = NvmlNative.DeviceGetPowerUsage(device, out uint powerMilliwatts);
		if (result == NvmlReturn.Success)
		{
			available = true;
			watts = powerMilliwatts / 1000.0;
			return;
		}
		available = false;
		watts = 0;
	}

}