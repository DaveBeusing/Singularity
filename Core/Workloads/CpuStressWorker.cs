// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Workloads;

public sealed class CpuStressWorker : IDisposable
{
	private readonly List<Task> workers = [];
	private CancellationTokenSource? cancellationTokenSource;

	public bool IsRunning => cancellationTokenSource is not null;

	public void Start(int threadCount)
	{
		if (IsRunning)
			return;

		cancellationTokenSource = new CancellationTokenSource();

		for (int i = 0; i < threadCount; i++)
		{
			CancellationToken token = cancellationTokenSource.Token;
			workers.Add(Task.Run(() => RunWorker(token), token));
		}
	}

	public void Stop()
	{
		if (cancellationTokenSource is null)
			return;
		cancellationTokenSource.Cancel();
		try
		{
			Task.WaitAll(workers.ToArray(), TimeSpan.FromSeconds(2));
		}
		catch
		{
		}

		workers.Clear();

		cancellationTokenSource.Dispose();
		cancellationTokenSource = null;
	}

	private static void RunWorker(
		CancellationToken cancellationToken)
	{
		double value = 0;

		while (!cancellationToken.IsCancellationRequested)
		{
			value += Math.Sqrt(
				Random.Shared.NextDouble());

			if (value > 1_000_000)
			{
				value = 0;
			}
		}
	}

	public void Dispose()
	{
		Stop();
	}

}