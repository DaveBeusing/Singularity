namespace Singularity.Monitoring;

/// <summary>
/// Momentaufnahme der aktuellen Systemwerte.
/// Diese Klasse enthält nur Daten und keine Logik.
/// </summary>
public sealed class SystemSnapshot
{
	public double ProcessCpuPercent { get; set; }

	public long ProcessMemoryMb { get; set; }

	public long TotalPhysicalMemoryMb { get; set; }

	public long AvailablePhysicalMemoryMb { get; set; }

	public long UsedPhysicalMemoryMb { get; set; }

	public double UsedPhysicalMemoryPercent { get; set; }
}