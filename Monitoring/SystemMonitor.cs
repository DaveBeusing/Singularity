using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Singularity.Monitoring;

/// <summary>
/// Liest aktuelle Prozess- und Systemspeicherwerte aus.
/// Es werden keine externen Bibliotheken verwendet.
/// Für den Arbeitsspeicher wird die Windows-API GlobalMemoryStatusEx genutzt.
/// </summary>
public sealed class SystemMonitor
{
	private readonly Process process = Process.GetCurrentProcess();

	private TimeSpan lastCpuTime;
	private DateTime lastSampleTime;

	public SystemMonitor()
	{
		lastCpuTime = process.TotalProcessorTime;
		lastSampleTime = DateTime.UtcNow;
	}

	public SystemSnapshot GetSnapshot()
	{
		process.Refresh();

		TimeSpan currentCpuTime = process.TotalProcessorTime;
		DateTime currentSampleTime = DateTime.UtcNow;

		double cpuUsedMs = (currentCpuTime - lastCpuTime).TotalMilliseconds;
		double elapsedMs = (currentSampleTime - lastSampleTime).TotalMilliseconds;

		double cpuPercent = 0;

		if (elapsedMs > 0)
		{
			cpuPercent = cpuUsedMs / (elapsedMs * Environment.ProcessorCount) * 100.0;
		}

		lastCpuTime = currentCpuTime;
		lastSampleTime = currentSampleTime;

		MemoryStatus memory = GetMemoryStatus();

		long totalMb = (long)(memory.TotalPhys / 1024 / 1024);
		long availableMb = (long)(memory.AvailPhys / 1024 / 1024);
		long usedMb = totalMb - availableMb;

		double usedPercent = totalMb > 0
			? usedMb / (double)totalMb * 100.0
			: 0;

		return new SystemSnapshot
		{
			ProcessCpuPercent = cpuPercent,
			ProcessMemoryMb = process.WorkingSet64 / 1024 / 1024,
			TotalPhysicalMemoryMb = totalMb,
			AvailablePhysicalMemoryMb = availableMb,
			UsedPhysicalMemoryMb = usedMb,
			UsedPhysicalMemoryPercent = usedPercent
		};
	}

	private static MemoryStatus GetMemoryStatus()
	{
		MemoryStatus status = new();
		status.Length = (uint)Marshal.SizeOf<MemoryStatus>();

		GlobalMemoryStatusEx(ref status);

		return status;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);

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
}