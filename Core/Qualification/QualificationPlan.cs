// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.Core.Workloads;

namespace Singularity.Core.Qualification;

public sealed record QualificationPlan(
	string Name,
	IReadOnlyList<QualificationStep> Steps,
	bool StopOnFailure)
{
	public static QualificationPlan CreateStandard(
		WorkloadOptions targets,
		QualificationProfile profile,
		bool stopOnFailure = false)
	{
		List<(string Name, WorkloadOptions Workload)> workloads = [];
		if (targets.EnableCpuWorkload)
			workloads.Add(("CPU", Copy(targets, cpu: true)));
		if (targets.EnableMemoryWorkload)
			workloads.Add(("MEMORY", Copy(targets, memory: true)));
		if (targets.EnableGpuWorkload)
			workloads.Add(("GPU", Copy(targets, gpu: true)));
		if (workloads.Count > 1)
			workloads.Add(("COMBINED", Copy(targets, cpu: targets.EnableCpuWorkload, memory: targets.EnableMemoryWorkload, gpu: targets.EnableGpuWorkload)));

		if (workloads.Count == 0)
			throw new InvalidOperationException("Select at least one workload for an automated run.");

		TimeSpan stepDuration = TimeSpan.FromTicks(profile.RecommendedDuration.Ticks / workloads.Count);
		QualificationStep[] steps = new QualificationStep[workloads.Count];
		for (int index = 0; index < workloads.Count; index++)
			steps[index] = new QualificationStep(workloads[index].Name, stepDuration, workloads[index].Workload);
		return new QualificationPlan($"{profile.Name} qualification", Array.AsReadOnly(steps), stopOnFailure);
	}

	private static WorkloadOptions Copy(WorkloadOptions source, bool cpu = false, bool memory = false, bool gpu = false) => new()
	{
		EnableCpuWorkload = cpu,
		EnableMemoryWorkload = memory,
		EnableGpuWorkload = gpu,
		CpuThreads = source.CpuThreads,
		MemoryGb = source.MemoryGb,
		GpuLoadPercent = source.GpuLoadPercent
	};
}
