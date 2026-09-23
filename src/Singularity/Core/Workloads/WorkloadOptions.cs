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

	/// <summary>
	/// Compatibility property for callers that target exactly one GPU.
	/// Multi-GPU callers should use <see cref="SelectedGpuIdentifiers"/>.
	/// </summary>
	public string? SelectedGpuIdentifier { get; set; }

	/// <summary>
	/// Stable GPU identifiers to exercise during this workload run.
	/// </summary>
	public IReadOnlyList<string> SelectedGpuIdentifiers { get; set; } =
		Array.Empty<string>();

	public IReadOnlyList<string> ResolveSelectedGpuIdentifiers()
	{
		IEnumerable<string> source = SelectedGpuIdentifiers.Count > 0
			? SelectedGpuIdentifiers
			: string.IsNullOrWhiteSpace(SelectedGpuIdentifier)
				? Array.Empty<string>()
				: [SelectedGpuIdentifier];

		return Array.AsReadOnly(
			source
				.Where(identifier => !string.IsNullOrWhiteSpace(identifier))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray());
	}
}
