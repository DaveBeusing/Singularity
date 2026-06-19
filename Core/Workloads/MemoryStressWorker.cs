// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Workloads;

public sealed class MemoryStressWorker : IDisposable
{
	private const int BlockSizeMb = 64;
	private const int BytesPerMb = 1024 * 1024;
	private const int BlockSizeBytes = BlockSizeMb * BytesPerMb;
	private const int PageSize = 4096;

	private readonly List<byte[]> allocatedBlocks = [];

	private Task? writeTask;
	private CancellationTokenSource? cancellationTokenSource;

	public bool IsRunning => cancellationTokenSource is not null;

	public long AllocatedMegabytes => allocatedBlocks.Count * BlockSizeMb;

	public void Start(int memoryGb)
	{
		if (IsRunning)
			return;

		int safeMemoryGb = Math.Max(1, memoryGb);
		int targetMb = safeMemoryGb * 1024;
		int blockCount = Math.Max(1, targetMb / BlockSizeMb);
		cancellationTokenSource = new CancellationTokenSource();

		try
		{
			for (int i = 0; i < blockCount; i++)
			{
				CancellationToken token = cancellationTokenSource.Token;
				byte[] block = new byte[BlockSizeBytes];
				TouchBlock(block, (byte)i, token);
				allocatedBlocks.Add(block);
			}
			writeTask = Task.Run(() => RunWriter(cancellationTokenSource.Token), cancellationTokenSource.Token);
		}
		catch
		{
			Stop();
			throw;
		}
	}

	public void Stop()
	{
		if (cancellationTokenSource is null)
			return;

		cancellationTokenSource.Cancel();

		try
		{
			writeTask?.Wait(TimeSpan.FromSeconds(5));
		}
		catch
		{
		}

		writeTask = null;

		allocatedBlocks.Clear();

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		cancellationTokenSource.Dispose();
		cancellationTokenSource = null;
	}

	private void RunWriter(CancellationToken cancellationToken)
	{
		byte value = 1;

		while (!cancellationToken.IsCancellationRequested)
		{
			foreach (byte[] block in allocatedBlocks)
			{
				if (cancellationToken.IsCancellationRequested)
					return;
				TouchBlock(block, value, cancellationToken);
				value++;
			}
		}
	}

	private static void TouchBlock(
		byte[] block,
		byte value,
		CancellationToken cancellationToken)
	{
		for (int i = 0; i < block.Length; i += PageSize)
		{
			if (cancellationToken.IsCancellationRequested)
				return;

			block[i] = value;
		}

		if (!cancellationToken.IsCancellationRequested)
		{
			block[^1] = value;
		}
	}

	public void Dispose()
	{
		Stop();
	}

}