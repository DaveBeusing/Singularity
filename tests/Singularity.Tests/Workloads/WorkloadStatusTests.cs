// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Workloads;

namespace Singularity.Tests.Workloads;

public sealed class WorkloadStatusTests
{
	[Theory]
	[InlineData(WorkloadState.Starting, true)]
	[InlineData(WorkloadState.Running, true)]
	[InlineData(WorkloadState.Stopping, true)]
	[InlineData(WorkloadState.Stopped, false)]
	[InlineData(WorkloadState.Failed, false)]
	public void IsRunning_ReflectsActiveLifecycleStates(WorkloadState state, bool expected)
	{
		WorkloadStatus status = new() { State = state };

		Assert.Equal(expected, status.IsRunning);
	}

	[Fact]
	public void ResolveSelectedGpuIdentifiers_PreservesMultiDeviceSelection()
	{
		WorkloadStatus status = new()
		{
			SelectedGpuIdentifier = "GPU-LEGACY",
			SelectedGpuIdentifiers = ["GPU-A", "GPU-B"]
		};

		Assert.Equal(["GPU-A", "GPU-B"], status.ResolveSelectedGpuIdentifiers());
	}

	[Fact]
	public void WorkloadOptions_UsesSingleGpuCompatibilitySelectionWhenNeeded()
	{
		WorkloadOptions options = new()
		{
			SelectedGpuIdentifier = "GPU-A"
		};

		Assert.Equal(["GPU-A"], options.ResolveSelectedGpuIdentifiers());
	}
}
