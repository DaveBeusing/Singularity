// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Singularity.Monitoring;

/// <summary>
/// Liest aktuelle Systemwerte aus.
/// CPU wird über GetSystemTimes gelesen.
/// RAM wird über GlobalMemoryStatusEx gelesen.
/// GPU wird über NVML gelesen.
/// </summary>
public sealed class SystemMonitor
{
	private readonly Process process = Process.GetCurrentProcess();
	private readonly NvmlGpuTelemetryProvider gpuTelemetryProvider = new();

	private TimeSpan lastProcessCpuTime;
	private DateTime lastProcessSampleTime;

	private ulong lastIdleTime;
	private ulong lastKernelTime;
	private ulong lastUserTime;
	private bool hasCpuSample;

	public SystemMonitor()
	{
		lastProcessCpuTime = process.TotalProcessorTime;
		lastProcessSampleTime = DateTime.UtcNow;
	}

	public SystemSnapshot GetSnapshot()
	{
		process.Refresh();

		MemoryStatus memory = GetMemoryStatus();
		GpuTelemetrySnapshot gpu = gpuTelemetryProvider.Read();

		long totalMb = (long)(memory.TotalPhys / 1024 / 1024);
		long availableMb = (long)(memory.AvailPhys / 1024 / 1024);
		long usedMb = totalMb - availableMb;

		double usedPercent = totalMb > 0 ? usedMb / (double)totalMb * 100.0 : 0;

		return new SystemSnapshot
		{
			CpuLoadPercent = GetCpuLoadPercent(),

			ProcessCpuPercent = GetProcessCpuPercent(),
			ProcessMemoryMb = process.WorkingSet64 / 1024 / 1024,

			TotalPhysicalMemoryMb = totalMb,
			AvailablePhysicalMemoryMb = availableMb,
			UsedPhysicalMemoryMb = usedMb,
			UsedPhysicalMemoryPercent = usedPercent,

			GpuTelemetryAvailable = gpu.IsAvailable,
			GpuLoadPercent = gpu.LoadPercent,
			GpuMemoryLoadPercent = gpu.MemoryLoadPercent,
			GpuTemperatureCelsius = gpu.TemperatureCelsius,
			GpuTelemetryStatus = gpu.Status
		};
	}

	private double GetProcessCpuPercent()
	{
		TimeSpan currentCpuTime = process.TotalProcessorTime;
		DateTime currentSampleTime = DateTime.UtcNow;

		double cpuUsedMs = (currentCpuTime - lastProcessCpuTime).TotalMilliseconds;
		double elapsedMs = (currentSampleTime - lastProcessSampleTime).TotalMilliseconds;

		double cpuPercent = 0;

		if (elapsedMs > 0)
		{
			cpuPercent = cpuUsedMs / (elapsedMs * Environment.ProcessorCount) * 100.0;
		}

		lastProcessCpuTime = currentCpuTime;
		lastProcessSampleTime = currentSampleTime;

		return Math.Clamp(cpuPercent, 0, 100);
	}

	private double GetCpuLoadPercent()
	{
		if (!GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime))
		{
			return 0;
		}

		ulong idle = ToUInt64(idleTime);
		ulong kernel = ToUInt64(kernelTime);
		ulong user = ToUInt64(userTime);

		if (!hasCpuSample)
		{
			lastIdleTime = idle;
			lastKernelTime = kernel;
			lastUserTime = user;
			hasCpuSample = true;
			return 0;
		}

		ulong idleDelta = idle - lastIdleTime;
		ulong kernelDelta = kernel - lastKernelTime;
		ulong userDelta = user - lastUserTime;

		ulong totalDelta = kernelDelta + userDelta;

		lastIdleTime = idle;
		lastKernelTime = kernel;
		lastUserTime = user;

		if (totalDelta == 0)
			return 0;

		double load = (totalDelta - idleDelta) / (double)totalDelta * 100.0;

		return Math.Clamp(load, 0, 100);
	}

	private static MemoryStatus GetMemoryStatus()
	{
		MemoryStatus status = new()
		{
			Length = (uint)Marshal.SizeOf<MemoryStatus>()
		};

		GlobalMemoryStatusEx(ref status);

		return status;
	}

	private static ulong ToUInt64(FileTime fileTime)
	{
		return ((ulong)fileTime.HighDateTime << 32) |
			fileTime.LowDateTime;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GetSystemTimes(
		out FileTime idleTime,
		out FileTime kernelTime,
		out FileTime userTime);

	[StructLayout(LayoutKind.Sequential)]
	private struct MemoryStatus
	{
		public uint Length;
		public uint MemoryLoad;
		public ulong TotalPhys;
		public ulong AvailPhys;
		public ulong TotalPageFile;
		public ulong AvailPageFile;
		public ulong TotalVirtual;
		public ulong AvailVirtual;
		public ulong AvailExtendedVirtual;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct FileTime
	{
		public uint LowDateTime;
		public uint HighDateTime;
	}

}