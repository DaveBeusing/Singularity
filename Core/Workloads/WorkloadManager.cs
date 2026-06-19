// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core;

namespace Singularity.Core.Workloads;

public sealed class WorkloadManager : IDisposable
{
	private readonly CpuStressWorker cpuStressWorker = new();
	private readonly MemoryStressWorker memoryStressWorker = new();

	private WorkloadState state = WorkloadState.Stopped;
	private string message = "Ready";

	private bool cpuEnabled;
	private bool memoryEnabled;
	private int cpuThreads;
	private int memoryGb;

	public bool IsRunning =>
		state is WorkloadState.Starting or WorkloadState.Running or WorkloadState.Stopping;

	public WorkloadStatus Status =>
		new()
		{
			State = state,
			CpuEnabled = cpuEnabled,
			MemoryEnabled = memoryEnabled,
			CpuThreads = cpuThreads,
			MemoryGb = memoryGb,
			MemoryAllocatedMb = memoryStressWorker.AllocatedMegabytes,
			Message = message
		};

	public void Start(WorkloadOptions options)
	{
		if (IsRunning)
			return;
		state = WorkloadState.Starting;
		message = "Starting";
		cpuEnabled = options.EnableCpuWorkload;
		memoryEnabled = options.EnableMemoryWorkload;
		cpuThreads = options.CpuThreads;
		memoryGb = options.MemoryGb;
		try
		{
			if (cpuEnabled)
			{
				cpuStressWorker.Start(cpuThreads);
			}
			if (memoryEnabled)
			{
				memoryStressWorker.Start(memoryGb);
			}
			state = WorkloadState.Running;
			message = BuildRunningMessage();
		}
		catch (Exception ex)
		{
			Stop();
			state = WorkloadState.Failed;
			message = ex.Message;
		}
	}

	public void Stop()
	{
		if (state == WorkloadState.Stopped)
			return;
		state = WorkloadState.Stopping;
		message = "Stopping";
		cpuStressWorker.Stop();
		memoryStressWorker.Stop();
		state = WorkloadState.Stopped;
		message = "Ready";
		cpuEnabled = false;
		memoryEnabled = false;
		cpuThreads = 0;
		memoryGb = 0;
	}

	public void ResetFailure()
	{
		if (state != WorkloadState.Failed)
			return;
		state = WorkloadState.Stopped;
		message = "Ready";
	}

	private string BuildRunningMessage()
	{
		List<string> parts = [];
		if (cpuEnabled)
		{
			parts.Add($"CPU {cpuThreads}T");
		}
		if (memoryEnabled)
		{
			parts.Add($"RAM {memoryGb}GB");
		}
		if (parts.Count == 0)
		{
			return "No workload selected";
		}
		return string.Join(" | ", parts);
	}

	public void Dispose()
	{
		Stop();
		cpuStressWorker.Dispose();
		memoryStressWorker.Dispose();
	}

}