// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Singularity.Core.Workloads;

public sealed class MemoryStressWorker : IDisposable
{
	private const int BlockSizeMb = 64;
	private const int BytesPerMb = 1024 * 1024;
	private const nuint BlockSizeBytes = BlockSizeMb * BytesPerMb;
	private const int PageSize = 4096;

	private readonly List<IntPtr> allocatedBlocks = [];

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
				IntPtr block = VirtualAlloc(IntPtr.Zero, BlockSizeBytes, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ReadWrite);

				if (block == IntPtr.Zero)
				{
					throw new OutOfMemoryException(
						$"Failed to allocate memory block {i + 1}/{blockCount}.");
				}
				allocatedBlocks.Add(block);
				TouchBlock(block, BlockSizeBytes, (byte)i, cancellationTokenSource.Token);
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

		foreach (IntPtr block in allocatedBlocks)
		{
			if (block != IntPtr.Zero)
			{
				VirtualFree(
					block,
					0,
					FreeType.Release);
			}
		}

		allocatedBlocks.Clear();

		cancellationTokenSource.Dispose();
		cancellationTokenSource = null;

		TrimWorkingSet();
	}

	private void RunWriter(CancellationToken cancellationToken)
	{
		byte value = 1;

		while (!cancellationToken.IsCancellationRequested)
		{
			foreach (IntPtr block in allocatedBlocks)
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				TouchBlock(block, BlockSizeBytes, value, cancellationToken);
				value++;
			}
		}
	}

	private static void TouchBlock(IntPtr block, nuint size, byte value, CancellationToken cancellationToken)
	{
		for (nuint offset = 0; offset < size; offset += PageSize)
		{
			if (cancellationToken.IsCancellationRequested)
				return;
			Marshal.WriteByte(IntPtr.Add(block, checked((int)offset)), value);
		}

		if (!cancellationToken.IsCancellationRequested)
		{
			Marshal.WriteByte(IntPtr.Add(block, checked((int)size - 1)), value);
		}
	}

	private static void TrimWorkingSet()
	{
		SetProcessWorkingSetSize(GetCurrentProcess(), new IntPtr(-1), new IntPtr(-1));
	}

	public void Dispose()
	{
		Stop();
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr VirtualAlloc(
		IntPtr lpAddress,
		nuint dwSize,
		AllocationType flAllocationType,
		MemoryProtection flProtect);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool VirtualFree(
		IntPtr lpAddress,
		nuint dwSize,
		FreeType dwFreeType);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetCurrentProcess();

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetProcessWorkingSetSize(
		IntPtr process,
		IntPtr minimumWorkingSetSize,
		IntPtr maximumWorkingSetSize);

	[Flags]
	private enum AllocationType : uint
	{
		Commit = 0x1000,
		Reserve = 0x2000
	}

	private enum MemoryProtection : uint
	{
		ReadWrite = 0x04
	}

	private enum FreeType : uint
	{
		Release = 0x8000
	}

}