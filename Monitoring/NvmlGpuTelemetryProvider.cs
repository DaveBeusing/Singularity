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

				double gpuLoad = ReadGpuLoad(device);
				double memoryLoad = ReadMemoryLoad(device);
				int temperature = ReadTemperature(device);

				return new GpuTelemetrySnapshot
				{
					IsAvailable = true,
					LoadPercent = gpuLoad,
					MemoryLoadPercent = memoryLoad,
					TemperatureCelsius = temperature,
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

	private static double ReadGpuLoad(IntPtr device)
	{
		NvmlReturn result = NvmlNative.DeviceGetUtilizationRates(device, out NvmlUtilization utilization);
		return result == NvmlReturn.Success ? utilization.Gpu : 0;
	}

	private static double ReadMemoryLoad(IntPtr device)
	{
		NvmlReturn result = NvmlNative.DeviceGetUtilizationRates(device, out NvmlUtilization utilization);
		return result == NvmlReturn.Success ? utilization.Memory : 0;
	}

	private static int ReadTemperature(IntPtr device)
	{
		NvmlReturn result = NvmlNative.DeviceGetTemperature(device, NvmlTemperatureSensor.Gpu, out uint temperature);
		return result == NvmlReturn.Success ? (int)temperature : 0;
	}

}