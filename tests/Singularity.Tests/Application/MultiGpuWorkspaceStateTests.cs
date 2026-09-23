// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Hardware.Models;
using Singularity.Monitoring.Models;

namespace Singularity.Tests.Application;

public sealed class MultiGpuWorkspaceStateTests
{
	[Fact]
	public void AvailableGpus_PreserveMultipleSelectionsAcrossReorder()
	{
		QualificationWorkspaceState state = new();
		GpuInventory gpuA = new() { Identifier = "GPU-A", Name = "GPU A" };
		GpuInventory gpuB = new() { Identifier = "GPU-B", Name = "GPU B" };
		state.SetAvailableGpus([gpuA, gpuB]);
		state.SetConfiguration(
			state.Configuration with
			{
				EnableGpuWorkload = true,
				SelectedGpuIdentifiers = ["GPU-A", "GPU-B"],
				SelectedGpuIdentifier = null
			});

		state.SetAvailableGpus([gpuB, gpuA]);

		Assert.Equal(
			["GPU-A", "GPU-B"],
			state.Configuration.ResolveSelectedGpuIdentifiers());
		Assert.Null(state.ValidateConfiguration());
	}

	[Fact]
	public void AvailableGpus_DeviceRemovalRequiresExplicitSelectionReview()
	{
		QualificationWorkspaceState state = new();
		GpuInventory gpuA = new() { Identifier = "GPU-A", Name = "GPU A" };
		GpuInventory gpuB = new() { Identifier = "GPU-B", Name = "GPU B" };
		state.SetAvailableGpus([gpuA, gpuB]);
		state.SetConfiguration(
			state.Configuration with
			{
				EnableGpuWorkload = true,
				SelectedGpuIdentifiers = ["GPU-A", "GPU-B"],
				SelectedGpuIdentifier = null
			});

		state.SetAvailableGpus([gpuA]);

		Assert.Equal(["GPU-A"], state.Configuration.ResolveSelectedGpuIdentifiers());
		string validationMessage = Assert.IsType<string>(state.ValidateConfiguration());
		Assert.Contains("Review and confirm", validationMessage);
		QualificationWorkspaceSnapshot blocked = state.CreateSnapshot(
			new QualificationCoordinator(new IdleWorkloadController()),
			new SystemSnapshot());
		Assert.False(blocked.CanStartManual);
		Assert.False(blocked.CanStartAutomated);

		state.SetConfiguration(state.Configuration);

		Assert.Null(state.ValidateConfiguration());
	}

	[Fact]
	public void Snapshot_ReportsPartialTelemetryGapWithoutSubstitutingAnotherGpu()
	{
		QualificationWorkspaceState state = new();
		state.SetAvailableGpus(
		[
			new GpuInventory { Identifier = "GPU-A", Name = "GPU A" },
			new GpuInventory { Identifier = "GPU-B", Name = "GPU B" }
		]);
		state.SetConfiguration(
			new QualificationConfiguration(
				false,
				1,
				false,
				1,
				true,
				90,
				QualificationProfiles.Standard)
			{
				SelectedGpuIdentifiers = ["GPU-A", "GPU-B"]
			});

		QualificationWorkspaceSnapshot snapshot = state.CreateSnapshot(
			new QualificationCoordinator(new IdleWorkloadController()),
			new SystemSnapshot
			{
				GpuTelemetrySnapshots =
				[
					new GpuTelemetrySnapshot
					{
						Identifier = "GPU-A",
						Name = "GPU A",
						IsAvailable = true,
						LoadPercent = 80,
						TemperatureCelsius = 55,
						Status = "OK"
					}
				]
			});

		Assert.True(snapshot.RequiredTelemetryUnavailable);
		Assert.Contains("1/2 available", snapshot.GpuTelemetry);
		Assert.NotNull(snapshot.Feedback);
		Assert.Contains("1 of 2 unavailable", snapshot.Feedback!.Message);
	}

	private sealed class IdleWorkloadController : IWorkloadController
	{
		public bool IsRunning => false;
		public WorkloadStatus Status { get; } = new();
		public void Start(WorkloadOptions options) { }
		public void Stop() { }
		public void ResetFailure() { }
	}
}
