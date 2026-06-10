namespace Singularity.Workloads;

/// <summary>
/// Gemeinsame Schnittstelle für alle Lastmodule.
/// Jedes Modul kann gestartet und gestoppt werden.
/// Beispiele: CPU-Workload, RAM-Workload, GPU-Workload.
/// </summary>
public interface IWorkload
{
	/// <summary>
	/// Anzeigename des Workloads.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gibt an, ob der Workload aktuell läuft.
	/// </summary>
	bool IsRunning { get; }

	/// <summary>
	/// Startet den Workload.
	/// Der CancellationToken erlaubt ein kontrolliertes Stoppen.
	/// </summary>
	void Start(CancellationToken token);

	/// <summary>
	/// Stoppt den Workload und gibt Ressourcen frei.
	/// </summary>
	void Stop();
}