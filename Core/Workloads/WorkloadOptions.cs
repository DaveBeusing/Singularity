// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Workloads;

/// <summary>
/// Defines the workloads and targets selected for a workload run.
/// </summary>
public sealed class WorkloadOptions
{
	public bool EnableCpuWorkload { get; set; }
	public bool EnableMemoryWorkload { get; set; }
	public bool EnableGpuWorkload { get; set; }
	public int CpuThreads { get; set; }
	public int MemoryGb { get; set; }
	public int GpuLoadPercent { get; set; }
}
