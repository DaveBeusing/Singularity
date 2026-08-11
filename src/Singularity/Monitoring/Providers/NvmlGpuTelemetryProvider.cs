// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Text;
using Singularity.Hardware.Native.Nvml;
using Singularity.Monitoring.Models;

namespace Singularity.Monitoring.Providers;

public sealed class NvmlGpuTelemetryProvider : IDisposable
{
	private sealed record DeviceContext(IntPtr Handle, string Identifier, string Name, int AdapterIndex);

	private readonly IReadOnlyList<DeviceContext> devices;
	private readonly GpuTelemetrySnapshot[] snapshots;
	private readonly string unavailableStatus;
	private bool initialized;
	private bool disposed;

	public NvmlGpuTelemetryProvider()
	{
		(devices, unavailableStatus) = Initialize();
		snapshots = new GpuTelemetrySnapshot[devices.Count];
		for (int index = 0; index < devices.Count; index++)
			snapshots[index] = Unavailable(devices[index], "Waiting for telemetry");
	}

	public IReadOnlyList<GpuTelemetrySnapshot> ReadFast()
	{
		if (!CanRead())
			return CreateUnavailableSnapshots();

		for (int index = 0; index < devices.Count; index++)
		{
			DeviceContext device = devices[index];
			try
			{
				NvmlReturn utilizationResult = NvmlNative.DeviceGetUtilizationRates(
					device.Handle, out NvmlUtilization utilization);
				NvmlReturn memoryResult = NvmlNative.DeviceGetMemoryInfo(
					device.Handle, out NvmlMemory memory);

				if (utilizationResult != NvmlReturn.Success || memoryResult != NvmlReturn.Success)
				{
					snapshots[index] = Unavailable(device, "GPU telemetry read failed");
					continue;
				}

				GpuTelemetrySnapshot previous = snapshots[index];
				snapshots[index] = new GpuTelemetrySnapshot
				{
					Identifier = device.Identifier,
					Name = device.Name,
					AdapterIndex = device.AdapterIndex,
					IsAvailable = true,
					LoadPercent = utilization.Gpu,
					MemoryControllerLoadPercent = utilization.Memory,
					MemoryTotalBytes = memory.Total,
					MemoryUsedBytes = memory.Used,
					MemoryFreeBytes = memory.Free,
					TemperatureCelsius = previous.TemperatureCelsius,
					PowerAvailable = previous.PowerAvailable,
					PowerWatts = previous.PowerWatts,
					Status = "OK"
				};
			}
			catch
			{
				snapshots[index] = Unavailable(device, "NVML read failed");
			}
		}

		return CopySnapshots();
	}

	public IReadOnlyList<GpuTelemetrySnapshot> ReadMedium()
	{
		if (!CanRead())
			return CreateUnavailableSnapshots();

		for (int index = 0; index < devices.Count; index++)
		{
			DeviceContext device = devices[index];
			try
			{
				NvmlReturn result = NvmlNative.DeviceGetTemperature(
					device.Handle, NvmlTemperatureSensor.Gpu, out uint temperature);
				if (result != NvmlReturn.Success)
				{
					snapshots[index] = Unavailable(device, "GPU telemetry read failed");
					continue;
				}

				ReadPower(device.Handle, out bool powerAvailable, out double powerWatts);
				GpuTelemetrySnapshot previous = snapshots[index];
				snapshots[index] = new GpuTelemetrySnapshot
				{
					Identifier = device.Identifier,
					Name = device.Name,
					AdapterIndex = device.AdapterIndex,
					IsAvailable = true,
					LoadPercent = previous.LoadPercent,
					MemoryControllerLoadPercent = previous.MemoryControllerLoadPercent,
					MemoryTotalBytes = previous.MemoryTotalBytes,
					MemoryUsedBytes = previous.MemoryUsedBytes,
					MemoryFreeBytes = previous.MemoryFreeBytes,
					TemperatureCelsius = (int)temperature,
					PowerAvailable = powerAvailable,
					PowerWatts = powerWatts,
					Status = "OK"
				};
			}
			catch
			{
				snapshots[index] = Unavailable(device, "NVML read failed");
			}
		}

		return CopySnapshots();
	}

	private bool CanRead() => !disposed && initialized && devices.Count > 0;

	private IReadOnlyList<GpuTelemetrySnapshot> CreateUnavailableSnapshots()
	{
		if (devices.Count == 0)
			return Array.Empty<GpuTelemetrySnapshot>();

		GpuTelemetrySnapshot[] unavailable = new GpuTelemetrySnapshot[devices.Count];
		for (int index = 0; index < devices.Count; index++)
			unavailable[index] = Unavailable(devices[index], disposed ? "NVML provider disposed" : unavailableStatus);
		return unavailable;
	}

	private IReadOnlyList<GpuTelemetrySnapshot> CopySnapshots() =>
		Array.AsReadOnly((GpuTelemetrySnapshot[])snapshots.Clone());

	private (IReadOnlyList<DeviceContext> Devices, string Status) Initialize()
	{
		try
		{
			NvmlReturn result = NvmlNative.Init();
			if (result != NvmlReturn.Success)
				return (Array.Empty<DeviceContext>(), "NVML init failed");

			initialized = true;
			result = NvmlNative.DeviceGetCount(out uint deviceCount);
			if (result != NvmlReturn.Success)
				return (Array.Empty<DeviceContext>(), "GPU enumeration failed");

			List<DeviceContext> contexts = new((int)deviceCount);
			for (uint index = 0; index < deviceCount; index++)
			{
				if (NvmlNative.DeviceGetHandleByIndex(index, out IntPtr handle) != NvmlReturn.Success)
					continue;

				string identifier = ReadString(handle, NvmlNative.DeviceGetUuid, 96);
				if (string.IsNullOrWhiteSpace(identifier))
					identifier = $"nvml:{index}";
				string name = ReadString(handle, NvmlNative.DeviceGetName, 96);
				contexts.Add(new DeviceContext(handle, identifier, name, (int)index));
			}

			return contexts.Count > 0
				? (contexts.AsReadOnly(), "OK")
				: (Array.Empty<DeviceContext>(), "GPU not found");
		}
		catch (DllNotFoundException) { return (Array.Empty<DeviceContext>(), "NVML not found"); }
		catch (EntryPointNotFoundException) { return (Array.Empty<DeviceContext>(), "NVML version unsupported"); }
		catch { return (Array.Empty<DeviceContext>(), "NVML initialization failed"); }
	}

	private delegate NvmlReturn ReadStringDelegate(IntPtr device, byte[] buffer, uint length);

	private static string ReadString(IntPtr device, ReadStringDelegate read, int capacity)
	{
		byte[] buffer = new byte[capacity];
		if (read(device, buffer, (uint)buffer.Length) != NvmlReturn.Success)
			return string.Empty;
		int length = Array.IndexOf(buffer, (byte)0);
		return Encoding.ASCII.GetString(buffer, 0, length < 0 ? buffer.Length : length).Trim();
	}

	private static GpuTelemetrySnapshot Unavailable(DeviceContext device, string status) => new()
	{
		Identifier = device.Identifier,
		Name = device.Name,
		AdapterIndex = device.AdapterIndex,
		IsAvailable = false,
		Status = status
	};

	private static void ReadPower(IntPtr device, out bool available, out double watts)
	{
		NvmlReturn result = NvmlNative.DeviceGetPowerUsage(device, out uint powerMilliwatts);
		available = result == NvmlReturn.Success;
		watts = available ? powerMilliwatts / 1000.0 : 0;
	}

	public void Dispose()
	{
		if (disposed)
			return;
		if (initialized)
		{
			try { NvmlNative.Shutdown(); }
			catch { }
		}
		initialized = false;
		disposed = true;
	}
}
