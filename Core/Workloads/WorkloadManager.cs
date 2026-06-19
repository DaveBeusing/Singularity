// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Workloads;

public sealed class WorkloadManager : IDisposable
{
	private readonly CpuStressWorker cpuWorker = new();

	public bool IsRunning => cpuWorker.IsRunning;

	public void Start(WorkloadOptions options)
	{
		if (options.EnableCpuWorkload)
		{
			cpuWorker.Start(options.CpuThreads);
		}
	}

	public void Stop()
	{
		cpuWorker.Stop();
	}

	public void Dispose()
	{
		Stop();
	}

}