// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Qualification;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Monitoring.Models;

namespace Singularity.Tests.Application;

public sealed class QualificationCoordinatorTests
{
	[Fact]
	public void ManualRun_ValidatesAndCreatesHistoryAndReport()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		WorkloadOptions options = new()
		{
			EnableCpuWorkload = true,
			CpuThreads = 4
		};

		bool started = coordinator.StartManual(options, QualificationProfiles.Quick);
		coordinator.Update(new SystemSnapshot { CpuLoadPercent = 100 });
		bool stopped = coordinator.Stop();

		Assert.True(started);
		Assert.True(stopped);
		Assert.Equal(QualificationSessionState.Completed, coordinator.Session.State);
		Assert.Equal(ValidationStatus.Pass, coordinator.Session.Result);
		Assert.Single(coordinator.History.Records);
		Assert.NotNull(coordinator.LastReport);
		Assert.Equal(ValidationStatus.Pass, coordinator.LastReport.OverallResult);
		Assert.Equal(1, coordinator.LastReport.TelemetryStatistics.CpuLoadPercent?.SampleCount);
		Assert.Equal(WorkloadState.Stopped, coordinator.WorkloadStatus.State);
	}

	[Fact]
	public void StartManual_RejectsSecondActiveRunWithoutResettingSession()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		WorkloadOptions options = new() { EnableCpuWorkload = true };

		Assert.True(coordinator.StartManual(options, QualificationProfiles.Standard));
		DateTime? originalStartTime = coordinator.Session.StartTime;

		Assert.False(coordinator.StartManual(options, QualificationProfiles.Quick));
		Assert.Equal(originalStartTime, coordinator.Session.StartTime);
		Assert.Equal(QualificationProfiles.Standard, coordinator.Session.Profile);
	}

	[Fact]
	public void Stop_CancelsAutomatedRunAndRecordsFailure()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		WorkloadOptions options = new() { EnableCpuWorkload = true };

		Assert.True(coordinator.StartAutomated(options, QualificationProfiles.Quick));
		Assert.True(coordinator.Stop());

		Assert.Equal(QualificationRunState.Cancelled, coordinator.Progress.State);
		Assert.Equal(QualificationSessionState.Failed, coordinator.Session.State);
		Assert.Equal(ValidationStatus.Fail, coordinator.Session.Result);
		Assert.Single(coordinator.History.Records);
		Assert.Null(coordinator.LastReport);
		Assert.Equal(WorkloadState.Stopped, coordinator.WorkloadStatus.State);
	}


	[Fact]
	public void Stop_CancelsMultiGpuAutomatedRunAndPreservesDeviceEvidence()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		WorkloadOptions options = new()
		{
			EnableGpuWorkload = true,
			GpuLoadPercent = 90,
			SelectedGpuIdentifiers = ["GPU-A", "GPU-B"]
		};

		Assert.True(coordinator.StartAutomated(options, QualificationProfiles.Quick));
		Assert.Equal(["GPU-A", "GPU-B"], coordinator.Session.SelectedGpuIdentifiers);

		Assert.True(coordinator.Stop());

		Assert.Equal(QualificationRunState.Cancelled, coordinator.Progress.State);
		Assert.Equal(QualificationSessionState.Failed, coordinator.Session.State);
		QualificationRecord record = Assert.Single(coordinator.History.Records);
		Assert.Equal(
			["GPU-A", "GPU-B"],
			record.GpuEvidence.Select(evidence => evidence.Identifier));
		Assert.All(record.GpuEvidence, evidence =>
			Assert.Equal(ValidationStatus.Unknown, evidence.Result));
	}

	[Fact]
	public void AutomatedGpuWorkloadFailure_PreservesOwningDeviceFailureEvidence()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		WorkloadOptions options = new()
		{
			EnableGpuWorkload = true,
			GpuLoadPercent = 90,
			SelectedGpuIdentifiers = ["GPU-A", "GPU-B"]
		};

		Assert.True(coordinator.StartAutomated(options, QualificationProfiles.Quick));
		workloads.FailGpu("GPU-B", "Direct3D 12 GPU workload timed out.");
		coordinator.Update(new SystemSnapshot
		{
			GpuTelemetrySnapshots =
			[
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-A",
					Name = "GPU A",
					IsAvailable = true,
					LoadPercent = 100,
					TemperatureCelsius = 50,
					Status = "OK"
				},
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-B",
					Name = "GPU B",
					IsAvailable = true,
					LoadPercent = 100,
					TemperatureCelsius = 50,
					Status = "OK"
				}
			]
		});

		Assert.Equal(QualificationRunState.Failed, coordinator.Progress.State);
		Assert.Equal(QualificationSessionState.Failed, coordinator.Session.State);
		Assert.NotNull(coordinator.LastReport);
		Assert.Equal(ValidationStatus.Fail, coordinator.LastReport!.OverallResult);
		Assert.Equal(ValidationStatus.Unknown, coordinator.LastReport.GpuEvidence[0].Result);
		Assert.Equal(ValidationStatus.Fail, coordinator.LastReport.GpuEvidence[1].Result);
		Assert.Contains("timed out", coordinator.LastReport.GpuEvidence[1].ValidationMessage);
	}

	[Fact]
	public void Stop_AfterGpuFailureBeforeNextUpdate_PreservesOwningDeviceFailureEvidence()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		WorkloadOptions options = new()
		{
			EnableGpuWorkload = true,
			GpuLoadPercent = 90,
			SelectedGpuIdentifiers = ["GPU-A", "GPU-B"]
		};
		SystemSnapshot telemetry = new()
		{
			GpuTelemetrySnapshots =
			[
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-A",
					Name = "GPU A",
					IsAvailable = true,
					LoadPercent = 100,
					TemperatureCelsius = 50,
					Status = "OK"
				},
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-B",
					Name = "GPU B",
					IsAvailable = true,
					LoadPercent = 100,
					TemperatureCelsius = 50,
					Status = "OK"
				}
			]
		};

		Assert.True(coordinator.StartManual(options, QualificationProfiles.Quick));
		coordinator.Update(telemetry);
		workloads.FailGpu("GPU-B", "Direct3D 12 GPU workload timed out.");

		Assert.True(coordinator.Stop());

		Assert.Equal(QualificationSessionState.Failed, coordinator.Session.State);
		Assert.NotNull(coordinator.LastReport);
		Assert.Equal(ValidationStatus.Unknown, coordinator.LastReport!.GpuEvidence[0].Result);
		Assert.Equal(ValidationStatus.Fail, coordinator.LastReport.GpuEvidence[1].Result);
		Assert.Contains("timed out", coordinator.LastReport.GpuEvidence[1].ValidationMessage);
	}

	[Fact]
	public void ManualWorkloadFailure_FinalizesActiveSession()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		WorkloadOptions options = new() { EnableCpuWorkload = true, CpuThreads = 4 };

		Assert.True(coordinator.StartManual(options, QualificationProfiles.Quick));
		workloads.Fail("CPU workload failed");

		coordinator.Update(new SystemSnapshot());

		Assert.Equal(QualificationSessionState.Failed, coordinator.Session.State);
		Assert.Equal(ValidationStatus.Fail, coordinator.Session.Result);
		Assert.Single(coordinator.History.Records);
		Assert.Null(coordinator.LastReport);
	}


	[Fact]
	public void Stop_WhenWorkloadAlreadyFailed_PreservesFailedSessionOutcome()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);
		WorkloadOptions options = new() { EnableCpuWorkload = true, CpuThreads = 4 };

		Assert.True(coordinator.StartManual(options, QualificationProfiles.Quick));
		coordinator.Update(new SystemSnapshot { CpuLoadPercent = 100 });
		workloads.Fail("CPU workload failed");

		Assert.True(coordinator.Stop());

		Assert.Equal(QualificationSessionState.Failed, coordinator.Session.State);
		Assert.Equal(ValidationStatus.Fail, coordinator.Session.Result);
		Assert.Single(coordinator.History.Records);
	}

	[Fact]
	public void StartAutomated_RejectsEmptyPlanWithoutStartingSession()
	{
		FakeWorkloadController workloads = new();
		QualificationCoordinator coordinator = new(workloads);

		Assert.Throws<InvalidOperationException>(() =>
			coordinator.StartAutomated(new WorkloadOptions(), QualificationProfiles.Quick));
		Assert.Equal(QualificationSessionState.Idle, coordinator.Session.State);
		Assert.Equal(0, workloads.StartCount);
	}

	private sealed class FakeWorkloadController : IWorkloadController
	{
		private WorkloadStatus status = new();

		public int StartCount { get; private set; }
		public bool IsRunning => status.IsRunning;
		public WorkloadStatus Status => status;

		public void Start(WorkloadOptions options)
		{
			StartCount++;
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
				SelectedGpuIdentifiers = options.ResolveSelectedGpuIdentifiers(),
				MemoryAllocatedMb = options.MemoryGb * 1024L,
				Message = "Running"
			};
		}

		public void Stop() => status = new WorkloadStatus();

		public void Fail(string message)
		{
			status = new WorkloadStatus
			{
				State = WorkloadState.Failed,
				Message = message
			};
		}

		public void FailGpu(string identifier, string message)
		{
			IReadOnlyList<string> selectedGpuIdentifiers = status.ResolveSelectedGpuIdentifiers();
			status = new WorkloadStatus
			{
				State = WorkloadState.Failed,
				GpuEnabled = true,
				GpuLoadPercent = status.GpuLoadPercent,
				SelectedGpuIdentifiers = selectedGpuIdentifiers,
				GpuDevices = selectedGpuIdentifiers
					.Select(selectedIdentifier => new GpuWorkloadDeviceStatus(
						selectedIdentifier,
						string.Equals(selectedIdentifier, identifier, StringComparison.OrdinalIgnoreCase)
							? WorkloadState.Failed
							: WorkloadState.Stopped,
						string.Equals(selectedIdentifier, identifier, StringComparison.OrdinalIgnoreCase)
							? $"GPU {selectedIdentifier} failed: {message}"
							: "Stopped after another GPU workload failed"))
					.ToArray(),
				Message = $"GPU {identifier} failed: {message}"
			};
		}

		public void ResetFailure()
		{
			if (status.State == WorkloadState.Failed)
				status = new WorkloadStatus();
		}
	}
}

