namespace Singularity.Workloads;

/// <summary>
/// Erzeugt CPU-Last durch parallele Rechenoperationen.
/// Für jeden gewünschten Thread wird eine eigene Endlosschleife gestartet.
/// </summary>
public sealed class CpuWorkload : IWorkload
{
	/// <summary>
	/// Anzahl der CPU-Threads, die belastet werden sollen.
	/// </summary>
	private readonly int threadCount;

	/// <summary>
	/// Liste aller gestarteten Hintergrundaufgaben.
	/// </summary>
	private readonly List<Task> workers = new();

	public string Name => "CPU";

	public bool IsRunning { get; private set; }

	public CpuWorkload(int threadCount)
	{
		this.threadCount = Math.Max(1, threadCount);
	}

	public void Start(CancellationToken token)
	{
		if (IsRunning)
			return;

		IsRunning = true;

		for (int i = 0; i < threadCount; i++)
		{
			workers.Add(Task.Run(() => BurnCpu(token), token));
		}
	}

	public void Stop()
	{
		IsRunning = false;
		workers.Clear();
	}

	/// <summary>
	/// Führt dauerhaft mathematische Operationen aus.
	/// Dadurch wird Rechenzeit auf der CPU verbraucht.
	/// </summary>
	private static void BurnCpu(CancellationToken token)
	{
		double value = 0.000001;

		while (!token.IsCancellationRequested)
		{
			value += Math.Sqrt(value + 1.2345);
			value *= Math.Sin(value) + Math.Cos(value);
			value += Math.Tan(0.00001);

			if (double.IsNaN(value) || double.IsInfinity(value) || value > 100_000)
			{
				value = 0.000001;
			}
		}
	}
}