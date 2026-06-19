// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Workloads;

public sealed class WorkloadStatus
{
	public WorkloadState State { get; init; } = WorkloadState.Stopped;
	public bool CpuEnabled { get; init; }
	public bool MemoryEnabled { get; init; }
	public int CpuThreads { get; init; }
	public int MemoryGb { get; init; }
	public long MemoryAllocatedMb { get; init; }
	public string Message { get; init; } = "Ready";

	public bool IsRunning => State is WorkloadState.Starting or WorkloadState.Running or WorkloadState.Stopping;

}