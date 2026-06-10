namespace Singularity.Workloads;

/// <summary>
/// Erzeugt RAM-Last.
/// Speicher wird blockweise reserviert und anschließend dauerhaft beschrieben.
/// Dadurch wird verhindert, dass Windows den Speicher nur virtuell reserviert,
/// ohne ihn wirklich physisch zu verwenden.
/// </summary>
public sealed class MemoryWorkload : IWorkload
{
	private readonly int memoryGb;
	private readonly List<byte[]> blocks = new();

	public string Name => "RAM";

	public bool IsRunning { get; private set; }

	public MemoryWorkload(int memoryGb)
	{
		this.memoryGb = Math.Max(1, memoryGb);
	}

	public void Start(CancellationToken token)
	{
		if (IsRunning)
			return;

		IsRunning = true;
		Task.Run(() => BurnMemory(token), token);
	}

	public void Stop()
	{
		IsRunning = false;

		// Durch Clear() geben wir die Referenzen auf die Speicherblöcke frei.
		// Der Garbage Collector kann den Speicher danach zurückholen.
		blocks.Clear();

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private void BurnMemory(CancellationToken token)
	{
		long targetBytes = (long)memoryGb * 1024 * 1024 * 1024;
		long allocatedBytes = 0;

		const int blockSize = 256 * 1024 * 1024;

		try
		{
			while (allocatedBytes < targetBytes && !token.IsCancellationRequested)
			{
				int currentBlockSize = (int)Math.Min(blockSize, targetBytes - allocatedBytes);

				byte[] block = new byte[currentBlockSize];

				TouchMemory(block);

				blocks.Add(block);
				allocatedBytes += currentBlockSize;

				Thread.Sleep(100);
			}

			while (!token.IsCancellationRequested)
			{
				foreach (byte[] block in blocks)
				{
					TouchMemory(block);
				}
			}
		}
		catch (OutOfMemoryException)
		{
			IsRunning = false;
		}
	}

	/// <summary>
	/// Beschreibt alle 4096 Bytes eine Speicherstelle.
	/// 4096 Bytes entsprechen typischerweise einer Speicherseite.
	/// Dadurch wird der Speicher real angefasst.
	/// </summary>
	private static void TouchMemory(byte[] block)
	{
		for (int i = 0; i < block.Length; i += 4096)
		{
			block[i]++;
		}
	}
}