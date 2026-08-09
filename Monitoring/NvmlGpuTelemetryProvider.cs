// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Native.Nvml;

namespace Singularity.Monitoring;

public sealed class NvmlGpuTelemetryProvider : IDisposable
{
	private readonly IReadOnlyList<IntPtr> deviceHandles;
	private readonly string unavailableStatus;
	private bool initialized;
	private bool disposed;

	public NvmlGpuTelemetryProvider()
	{
		(deviceHandles, unavailableStatus) = Initialize();
	}

	public GpuTelemetrySnapshot ReadFast()
	{
		return Read(device =>
		{
			NvmlReturn utilizationResult =
				NvmlNative.DeviceGetUtilizationRates(device, out NvmlUtilization utilization);
			NvmlReturn memoryResult =
				NvmlNative.DeviceGetMemoryInfo(device, out NvmlMemory memory);

			if (utilizationResult != NvmlReturn.Success ||
				memoryResult != NvmlReturn.Success)
			{
				return Unavailable("GPU telemetry read failed");
			}

			return new GpuTelemetrySnapshot
			{
				IsAvailable = true,
				LoadPercent = utilization.Gpu,
				MemoryControllerLoadPercent = utilization.Memory,
				MemoryTotalBytes = memory.Total,
				MemoryUsedBytes = memory.Used,
				MemoryFreeBytes = memory.Free,
				Status = "OK"
			};
		});
	}

	public GpuTelemetrySnapshot ReadMedium()
	{
		return Read(device =>
		{
			NvmlReturn temperatureResult = NvmlNative.DeviceGetTemperature(
				device,
				NvmlTemperatureSensor.Gpu,
				out uint temperature);

			if (temperatureResult != NvmlReturn.Success)
				return Unavailable("GPU telemetry read failed");

			ReadPower(device, out bool powerAvailable, out double powerWatts);

			return new GpuTelemetrySnapshot
			{
				IsAvailable = true,
				TemperatureCelsius = (int)temperature,
				PowerAvailable = powerAvailable,
				PowerWatts = powerWatts,
				Status = "OK"
			};
		});
	}

	private GpuTelemetrySnapshot Read(Func<IntPtr, GpuTelemetrySnapshot> readTelemetry)
	{
		if (disposed)
			return Unavailable("NVML provider disposed");

		if (!initialized || deviceHandles.Count == 0)
			return Unavailable(unavailableStatus);

		try
		{
			return readTelemetry(deviceHandles[0]);
		}
		catch { return Unavailable("NVML read failed"); }
	}

	private (IReadOnlyList<IntPtr> Handles, string Status) Initialize()
	{
		try
		{
			NvmlReturn result = NvmlNative.Init();
			if (result != NvmlReturn.Success)
				return (Array.Empty<IntPtr>(), "NVML init failed");

			initialized = true;
			result = NvmlNative.DeviceGetCount(out uint deviceCount);
			if (result != NvmlReturn.Success)
				return (Array.Empty<IntPtr>(), "GPU enumeration failed");

			List<IntPtr> handles = new((int)deviceCount);
			for (uint index = 0; index < deviceCount; index++)
			{
				if (NvmlNative.DeviceGetHandleByIndex(index, out IntPtr device) == NvmlReturn.Success)
					handles.Add(device);
			}

			return handles.Count > 0
				? (handles.AsReadOnly(), "OK")
				: (Array.Empty<IntPtr>(), "GPU not found");
		}
		catch (DllNotFoundException)
		{
			return (Array.Empty<IntPtr>(), "NVML not found");
		}
		catch (EntryPointNotFoundException)
		{
			return (Array.Empty<IntPtr>(), "NVML version unsupported");
		}
		catch
		{
			return (Array.Empty<IntPtr>(), "NVML initialization failed");
		}
	}

	private static GpuTelemetrySnapshot Unavailable(string status) =>
		new() { IsAvailable = false, Status = status };

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

	public void Dispose()
	{
		if (disposed)
			return;

		if (initialized)
		{
			try
			{
				NvmlNative.Shutdown();
			}
			catch
			{
				// NVML may already be unavailable during process shutdown.
			}
		}

		initialized = false;
		disposed = true;
	}

}
