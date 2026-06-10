namespace Singularity.Core;

/// <summary>
/// Enthält alle Optionen, die der Nutzer in der Oberfläche auswählt.
/// Diese Klasse wird vom UI an den WorkloadController übergeben.
/// </summary>
public sealed class WorkloadOptions
{
	public bool EnableCpuWorkload { get; set; }
	public bool EnableMemoryWorkload { get; set; }
	public bool EnableGpuWorkload { get; set; }

	public int CpuThreads { get; set; }
	public int MemoryGb { get; set; }
}