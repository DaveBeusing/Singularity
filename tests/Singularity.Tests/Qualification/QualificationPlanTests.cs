// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Qualification;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;

namespace Singularity.Tests.Qualification;

public sealed class QualificationPlanTests
{
	[Fact]
	public void CreateStandard_BuildsDedicatedAndCombinedSteps()
	{
		WorkloadOptions options = new()
		{
			EnableCpuWorkload = true,
			EnableMemoryWorkload = true,
			EnableGpuWorkload = true,
			CpuThreads = 8,
			MemoryGb = 4,
			GpuLoadPercent = 90
		};

		QualificationPlan plan = QualificationPlan.CreateStandard(options, QualificationProfiles.Quick, stopOnFailure: true);

		Assert.Equal(["CPU", "MEMORY", "GPU", "COMBINED"], plan.Steps.Select(step => step.Name));
		Assert.True(plan.StopOnFailure);
		Assert.Equal(QualificationProfiles.Quick.RecommendedDuration, TimeSpan.FromTicks(plan.Steps.Sum(step => step.Duration.Ticks)));
		Assert.True(plan.Steps[^1].Workload.EnableCpuWorkload);
		Assert.True(plan.Steps[^1].Workload.EnableMemoryWorkload);
		Assert.True(plan.Steps[^1].Workload.EnableGpuWorkload);
	}

	[Fact]
	public void CreateStandard_RejectsEmptySelection()
	{
		WorkloadOptions options = new();

		Assert.Throws<InvalidOperationException>(() =>
			QualificationPlan.CreateStandard(options, QualificationProfiles.Standard));
	}
}
