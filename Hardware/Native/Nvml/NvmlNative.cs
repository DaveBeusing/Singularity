// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Singularity.Hardware.Native.Nvml;

internal static class NvmlNative
{
	[DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
	public static extern NvmlReturn Init();

	[DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
	public static extern NvmlReturn Shutdown();

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
	public static extern NvmlReturn DeviceGetHandleByIndex(uint index, out IntPtr device);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetName")]
	public static extern NvmlReturn DeviceGetName(IntPtr device, byte[] name, uint length);

	[DllImport("nvml.dll", EntryPoint = "nvmlSystemGetDriverVersion")]
	public static extern NvmlReturn SystemGetDriverVersion(byte[] version, uint length);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMemoryInfo")]
	public static extern NvmlReturn DeviceGetMemoryInfo(IntPtr device, out NvmlMemory memory);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
	public static extern NvmlReturn DeviceGetTemperature(IntPtr device, NvmlTemperatureSensor sensorType, out uint temperature);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates")]
	public static extern NvmlReturn DeviceGetUtilizationRates(IntPtr device,out NvmlUtilization utilization);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCurrPcieLinkGeneration")]
	public static extern NvmlReturn DeviceGetCurrPcieLinkGeneration(IntPtr device, out uint generation);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMaxPcieLinkGeneration")]
	public static extern NvmlReturn DeviceGetMaxPcieLinkGeneration(IntPtr device, out uint generation);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCurrPcieLinkWidth")]
	public static extern NvmlReturn DeviceGetCurrPcieLinkWidth(IntPtr device, out uint width);

	[DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMaxPcieLinkWidth")]
	public static extern NvmlReturn DeviceGetMaxPcieLinkWidth(IntPtr device, out uint width);

}