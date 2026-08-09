// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Singularity.Monitoring;

/// <summary>
/// Samples system telemetry in the background and exposes the latest cached snapshot.
/// </summary>
public sealed class SystemMonitor : IDisposable
{
	private readonly Process process = Process.GetCurrentProcess();
	private readonly NvmlGpuTelemetryProvider gpuTelemetryProvider = new();
	private readonly LibreHardwareCpuTelemetryProvider cpuTelemetryProvider = new();
	private readonly TelemetryCache cache = new();
	private readonly TelemetryScheduler scheduler;

	private TimeSpan lastProcessCpuTime;
	private DateTime lastProcessSampleTime;
	private ulong lastIdleTime;
	private ulong lastKernelTime;
	private ulong lastUserTime;
	private bool hasCpuSample;
	private bool disposed;

	public SystemMonitor()
	{
		lastProcessCpuTime = process.TotalProcessorTime;
		lastProcessSampleTime = DateTime.UtcNow;
		scheduler = new TelemetryScheduler(SampleFast, SampleMedium, SampleSlow);
	}

	public SystemSnapshot GetSnapshot()
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		return cache.GetSnapshot();
	}

	private void SampleFast()
	{
		process.Refresh();
		MemoryStatus memory = GetMemoryStatus();
		GpuTelemetrySnapshot gpu = gpuTelemetryProvider.ReadFast();
		long totalMb = (long)(memory.TotalPhys / 1024 / 1024);
		long availableMb = (long)(memory.AvailPhys / 1024 / 1024);
		long usedMb = totalMb - availableMb;

		cache.Update(snapshot =>
		{
			snapshot.CpuLoadPercent = GetCpuLoadPercent();
			snapshot.ProcessCpuPercent = GetProcessCpuPercent();
			snapshot.ProcessMemoryMb = process.WorkingSet64 / 1024 / 1024;
			snapshot.TotalPhysicalMemoryMb = totalMb;
			snapshot.AvailablePhysicalMemoryMb = availableMb;
			snapshot.UsedPhysicalMemoryMb = usedMb;
			snapshot.UsedPhysicalMemoryPercent = totalMb > 0 ? usedMb / (double)totalMb * 100.0 : 0;
			snapshot.GpuTelemetryAvailable = gpu.IsAvailable;
			snapshot.GpuLoadPercent = gpu.LoadPercent;
			snapshot.GpuMemoryControllerLoadPercent = gpu.MemoryControllerLoadPercent;
			snapshot.GpuMemoryUsedPercent = gpu.MemoryUsedPercent;
			snapshot.GpuMemoryUsedMb = ToMegabytes(gpu.MemoryUsedBytes);
			snapshot.GpuMemoryTotalMb = ToMegabytes(gpu.MemoryTotalBytes);
			snapshot.GpuTelemetryStatus = gpu.Status;
		});
	}

	private void SampleMedium()
	{
		GpuTelemetrySnapshot gpu = gpuTelemetryProvider.ReadMedium();
		cache.Update(snapshot =>
		{
			if (!gpu.IsAvailable)
			{
				snapshot.GpuTelemetryAvailable = false;
				snapshot.GpuTelemetryStatus = gpu.Status;
				return;
			}

			snapshot.GpuTelemetryAvailable = true;
			snapshot.GpuTemperatureCelsius = gpu.TemperatureCelsius;
			snapshot.GpuPowerAvailable = gpu.PowerAvailable;
			snapshot.GpuPowerWatts = gpu.PowerWatts;
			snapshot.GpuTelemetryStatus = gpu.Status;
		});
	}

	private void SampleSlow()
	{
		CpuTelemetrySnapshot cpu = cpuTelemetryProvider.Read();
		cache.Update(snapshot =>
		{
			snapshot.CpuTemperatureAvailable = cpu.IsAvailable;
			snapshot.CpuTemperatureCelsius = cpu.TemperatureCelsius;
			snapshot.CpuTemperatureStatus = cpu.Status;
		});
	}

	private double GetProcessCpuPercent()
	{
		TimeSpan currentCpuTime = process.TotalProcessorTime;
		DateTime currentSampleTime = DateTime.UtcNow;
		double elapsedMs = (currentSampleTime - lastProcessSampleTime).TotalMilliseconds;
		double cpuPercent = elapsedMs > 0
			? (currentCpuTime - lastProcessCpuTime).TotalMilliseconds / (elapsedMs * Environment.ProcessorCount) * 100.0
			: 0;
		lastProcessCpuTime = currentCpuTime;
		lastProcessSampleTime = currentSampleTime;
		return Math.Clamp(cpuPercent, 0, 100);
	}

	private double GetCpuLoadPercent()
	{
		if (!GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime))
			return 0;

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
		ulong totalDelta = kernel - lastKernelTime + user - lastUserTime;
		lastIdleTime = idle;
		lastKernelTime = kernel;
		lastUserTime = user;
		return totalDelta == 0 ? 0 : Math.Clamp((totalDelta - idleDelta) / (double)totalDelta * 100.0, 0, 100);
	}

	private static MemoryStatus GetMemoryStatus()
	{
		MemoryStatus status = new() { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
		GlobalMemoryStatusEx(ref status);
		return status;
	}

	private static ulong ToUInt64(FileTime value) => ((ulong)value.HighDateTime << 32) | value.LowDateTime;
	private static long ToMegabytes(ulong bytes) => (long)(bytes / 1024 / 1024);

	public void Dispose()
	{
		if (disposed)
			return;
		disposed = true;
		scheduler.Dispose();
		gpuTelemetryProvider.Dispose();
		cpuTelemetryProvider.Dispose();
		process.Dispose();
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

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
