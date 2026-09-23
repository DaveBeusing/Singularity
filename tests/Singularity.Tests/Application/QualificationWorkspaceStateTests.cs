// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Globalization;

using Singularity.Application;
using Singularity.Core.Qualification;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Hardware.Models;
using Singularity.Monitoring.Models;
using Singularity.UI.Navigation;

namespace Singularity.Tests.Application;

public sealed class QualificationWorkspaceStateTests
{
	[Fact]
	public void IdleState_AllowsStartAndDisablesStop()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		QualificationWorkspaceState state = new();

		QualificationWorkspaceSnapshot snapshot = state.CreateSnapshot(
			coordinator,
			new SystemSnapshot { TotalPhysicalMemoryMb = 16384 });

		Assert.True(snapshot.CanStartManual);
		Assert.True(snapshot.CanStartAutomated);
		Assert.False(snapshot.CanStop);
		Assert.Equal("READY", snapshot.OverallState);
	}

	[Fact]
	public void ManualRun_MapsCurrentSessionAndCommandAvailability()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		QualificationWorkspaceState state = new();
		QualificationConfiguration configuration = CpuOnly(QualificationProfiles.Quick);
		state.SetConfiguration(configuration);

		Assert.True(coordinator.StartManual(configuration.ToWorkloadOptions(), configuration.Profile));
		state.MarkStarted(QualificationMode.Manual);

		QualificationWorkspaceSnapshot snapshot = state.CreateSnapshot(
			coordinator,
			new SystemSnapshot { CpuLoadPercent = 95 });

		Assert.Equal(QualificationMode.Manual, snapshot.Mode);
		Assert.Equal(QualificationSessionState.Running, snapshot.SessionState);
		Assert.Equal(QualificationProfiles.Quick, snapshot.Configuration.Profile);
		Assert.NotNull(snapshot.StartedAt);
		Assert.Equal("RUNNING", snapshot.OverallState);
		Assert.False(snapshot.CanStartManual);
		Assert.False(snapshot.CanStartAutomated);
		Assert.True(snapshot.CanStop);
	}

	[Fact]
	public void AutomatedRun_MapsStepProgressAndCommandAvailability()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		QualificationWorkspaceState state = new();
		QualificationConfiguration configuration = CpuOnly(QualificationProfiles.Standard);
		state.SetConfiguration(configuration);

		Assert.True(coordinator.StartAutomated(configuration.ToWorkloadOptions(), configuration.Profile));
		state.MarkStarted(QualificationMode.Automated);

		QualificationWorkspaceSnapshot snapshot = state.CreateSnapshot(
			coordinator,
			new SystemSnapshot { CpuLoadPercent = 95 });

		Assert.Equal(QualificationMode.Automated, snapshot.Mode);
		Assert.Equal(QualificationRunState.Running, snapshot.AutomatedState);
		Assert.Equal("CPU", snapshot.ActiveStep);
		Assert.Equal(1, snapshot.StepNumber);
		Assert.Equal(1, snapshot.StepCount);
		Assert.InRange(snapshot.ProgressPercent, 0, 100);
		Assert.False(snapshot.CanStartManual);
		Assert.False(snapshot.CanStartAutomated);
		Assert.True(snapshot.CanStop);
	}

	[Fact]
	public void NavigationAwayAndBack_DoesNotResetQualificationConfiguration()
	{
		QualificationWorkspaceState state = new();
		QualificationConfiguration configuration = new(
			true,
			12,
			false,
			4,
			true,
			87,
			QualificationProfiles.BurnIn);
		state.SetConfiguration(configuration);

		NavigationService navigation = new(WorkspaceCatalog.CreateDefault());
		Assert.True(navigation.Navigate(WorkspaceId.Qualification));
		Assert.True(navigation.Navigate(WorkspaceId.Platform));
		Assert.True(navigation.Navigate(WorkspaceId.Qualification));

		Assert.Equal(configuration, state.Configuration);
	}


	[Fact]
	public void CompletedSession_KeepsProfileUsedForRunAfterConfigurationChanges()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		QualificationWorkspaceState state = new();
		QualificationConfiguration quick = CpuOnly(QualificationProfiles.Quick);
		state.SetConfiguration(quick);

		Assert.True(coordinator.StartManual(quick.ToWorkloadOptions(), quick.Profile));
		state.MarkStarted(QualificationMode.Manual);
		coordinator.Update(new SystemSnapshot { CpuLoadPercent = 100 });
		Assert.True(coordinator.Stop());

		state.SetConfiguration(CpuOnly(QualificationProfiles.BurnIn));
		QualificationWorkspaceSnapshot snapshot = state.CreateSnapshot(
			coordinator,
			new SystemSnapshot { CpuLoadPercent = 0 });

		Assert.Equal("Quick", snapshot.SessionProfile);
		Assert.Equal(QualificationProfiles.BurnIn, snapshot.Configuration.Profile);
	}

	[Fact]
	public void WorkloadFailure_MapsFailureStateAndMessage()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		QualificationWorkspaceState state = new();
		QualificationConfiguration configuration = CpuOnly(QualificationProfiles.Quick);
		state.SetConfiguration(configuration);

		Assert.True(coordinator.StartManual(configuration.ToWorkloadOptions(), configuration.Profile));
		state.MarkStarted(QualificationMode.Manual);
		workloads.Fail("CPU workload failed");
		coordinator.Update(new SystemSnapshot());

		QualificationWorkspaceSnapshot snapshot = state.CreateSnapshot(
			coordinator,
			new SystemSnapshot());

		Assert.Equal("FAILED", snapshot.OverallState);
		Assert.Equal(QualificationSessionState.Failed, snapshot.SessionState);
		Assert.NotNull(snapshot.Feedback);
		Assert.Equal(QualificationFeedbackLevel.Failure, snapshot.Feedback!.Level);
		Assert.Equal("CPU workload failed", snapshot.Feedback.Message);
	}

	[Fact]
	public void AvailableGpu_DefaultsToSingleStableDevice()
	{
		QualificationWorkspaceState state = new();

		state.SetAvailableGpus(
		[
			new GpuInventory
			{
				Identifier = "GPU-A",
				Name = "Example GPU"
			}
		]);

		Assert.Equal("GPU-A", state.Configuration.SelectedGpuIdentifier);
		Assert.Single(state.AvailableGpus);
	}

	[Fact]
	public void AvailableGpu_PreservesExplicitSelectionAcrossReordering()
	{
		QualificationWorkspaceState state = new();
		GpuInventory gpuA = new() { Identifier = "GPU-A", Name = "GPU A" };
		GpuInventory gpuB = new() { Identifier = "GPU-B", Name = "GPU B" };
		state.SetAvailableGpus([gpuA, gpuB]);
		state.SetConfiguration(
			state.Configuration with
			{
				EnableGpuWorkload = true,
				SelectedGpuIdentifier = "GPU-B",
				SelectedGpuName = "GPU B"
			});

		state.SetAvailableGpus([gpuB, gpuA]);

		Assert.Equal("GPU-B", state.Configuration.SelectedGpuIdentifier);
	}

	[Fact]
	public void AvailableGpu_ClearsStaleSelectionAfterRemoval()
	{
		QualificationWorkspaceState state = new();
		GpuInventory gpuA = new() { Identifier = "GPU-A", Name = "GPU A" };
		GpuInventory gpuB = new() { Identifier = "GPU-B", Name = "GPU B" };
		state.SetAvailableGpus([gpuA, gpuB]);
		state.SetConfiguration(
			state.Configuration with
			{
				EnableGpuWorkload = true,
				SelectedGpuIdentifier = "GPU-B",
				SelectedGpuName = "GPU B"
			});

		state.SetAvailableGpus([gpuA]);

		Assert.Null(state.Configuration.SelectedGpuIdentifier);
		Assert.NotNull(state.CreateSnapshot(
			new QualificationCoordinator(new FakeWorkloadController()),
			new SystemSnapshot()).Feedback);
	}

	[Fact]
	public void SelectedGpuTelemetry_DoesNotUseFirstGpuCompatibilityFields()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		QualificationWorkspaceState state = new();
		GpuInventory gpuA = new() { Identifier = "GPU-A", Name = "GPU A" };
		GpuInventory gpuB = new() { Identifier = "GPU-B", Name = "GPU B" };
		state.SetAvailableGpus([gpuA, gpuB]);
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
				SelectedGpuIdentifier = "GPU-B",
				SelectedGpuName = "GPU B"
			});

		QualificationWorkspaceSnapshot snapshot = state.CreateSnapshot(
			coordinator,
			new SystemSnapshot
			{
				GpuTelemetryAvailable = false,
				GpuTelemetryStatus = "First GPU unavailable",
				GpuTelemetrySnapshots =
				[
					new GpuTelemetrySnapshot
					{
						Identifier = "GPU-A",
						IsAvailable = false,
						Status = "First GPU unavailable"
					},
					new GpuTelemetrySnapshot
					{
						Identifier = "GPU-B",
						IsAvailable = true,
						LoadPercent = 73,
						TemperatureCelsius = 58,
						Status = "OK"
					}
				]
			});

		Assert.False(snapshot.RequiredTelemetryUnavailable);
		string expectedLoad = 73d.ToString("0.0", CultureInfo.CurrentCulture);
		Assert.StartsWith($"{expectedLoad} %", snapshot.GpuTelemetry, StringComparison.Ordinal);
		Assert.Equal("GPU-B", snapshot.Configuration.SelectedGpuIdentifier);
	}

	[Fact]
	public void RequiredGpuTelemetryUnavailable_MapsPersistentWarning()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		QualificationWorkspaceState state = new();
		state.SetConfiguration(new QualificationConfiguration(
			false,
			1,
			false,
			1,
			true,
			90,
			QualificationProfiles.Standard));

		QualificationWorkspaceSnapshot snapshot = state.CreateSnapshot(
			coordinator,
			new SystemSnapshot { GpuTelemetryStatus = "NVML unavailable" });

		Assert.True(snapshot.RequiredTelemetryUnavailable);
		Assert.NotNull(snapshot.Feedback);
		Assert.Equal(QualificationFeedbackLevel.Warning, snapshot.Feedback!.Level);
		Assert.Contains("GPU telemetry", snapshot.Feedback.Message);
	}

	private static QualificationConfiguration CpuOnly(QualificationProfile profile)
	{
		return new QualificationConfiguration(
			true,
			8,
			false,
			1,
			false,
			1,
			profile);
	}

	private sealed class FakeWorkloadController : IWorkloadController
	{
		private WorkloadStatus status = new();

		public bool IsRunning => status.IsRunning;
		public WorkloadStatus Status => status;

		public void Start(WorkloadOptions options)
		{
			status = new WorkloadStatus
			{
				State = WorkloadState.Running,
				CpuEnabled = options.EnableCpuWorkload,
				MemoryEnabled = options.EnableMemoryWorkload,
				GpuEnabled = options.EnableGpuWorkload,
				CpuThreads = options.CpuThreads,
				MemoryGb = options.MemoryGb,
				GpuLoadPercent = options.GpuLoadPercent,
				SelectedGpuIdentifier = options.SelectedGpuIdentifier,
				MemoryAllocatedMb = options.MemoryGb * 1024L,
				Message = "Running"
			};
		}

		public void Stop()
		{
			status = new WorkloadStatus();
		}

		public void ResetFailure()
		{
			if (status.State == WorkloadState.Failed)
				status = new WorkloadStatus();
		}

		public void Fail(string message)
		{
			status = new WorkloadStatus
			{
				State = WorkloadState.Failed,
				Message = message
			};
		}
	}
}
