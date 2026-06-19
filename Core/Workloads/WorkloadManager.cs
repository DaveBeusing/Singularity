// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core;

namespace Singularity.Core.Workloads;

public sealed class WorkloadManager : IDisposable
{
	private readonly CpuStressWorker cpuStressWorker = new();
	private readonly MemoryStressWorker memoryStressWorker = new();

	public bool IsRunning =>
		cpuStressWorker.IsRunning ||
		memoryStressWorker.IsRunning;

	public void Start(WorkloadOptions options)
	{
		if (IsRunning)
			return;

		if (options.EnableCpuWorkload)
		{
			cpuStressWorker.Start(options.CpuThreads);
		}

		if (options.EnableMemoryWorkload)
		{
			memoryStressWorker.Start(options.MemoryGb);
		}
	}

	public void Stop()
	{
		cpuStressWorker.Stop();
		memoryStressWorker.Stop();
	}

	public void Dispose()
	{
		Stop();

		cpuStressWorker.Dispose();
		memoryStressWorker.Dispose();
	}
}