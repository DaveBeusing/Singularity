// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Hardware.Models;

namespace Singularity.Tests.Application;

public sealed class QualificationGpuSelectionTests
{
	[Fact]
	public void CreateOptions_FiltersTransientFallbackIdentity()
	{
		IReadOnlyList<QualificationGpuOption> options =
			QualificationGpuSelection.CreateOptions(
			[
				new GpuInventory { Identifier = "nvml:0", Name = "Transient" },
				new GpuInventory { Identifier = "GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE", Name = "Stable" }
			]);

		QualificationGpuOption option = Assert.Single(options);
		Assert.Equal("GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE", option.Identifier);
	}

	[Fact]
	public void ResolveSelection_DefaultsDeterministicallyAndUsesIdentifierAfterReorder()
	{
		QualificationGpuOption gpuA = new("GPU-A", "GPU A");
		QualificationGpuOption gpuB = new("GPU-B", "GPU B");

		Assert.Equal(gpuA, QualificationGpuSelection.ResolveSelection([gpuA, gpuB], null));
		Assert.Equal(gpuB, QualificationGpuSelection.ResolveSelection([gpuB, gpuA], "GPU-B"));
	}

	[Fact]
	public void ResolveSelection_ReturnsNullForStaleIdentifier()
	{
		QualificationGpuOption gpuA = new("GPU-A", "GPU A");

		Assert.Null(QualificationGpuSelection.ResolveSelection([gpuA], "GPU-REMOVED"));
	}
}
