// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Monitoring.Models;

namespace Singularity.Tests.Validation;

public sealed class MultiGpuValidationTests
{
	[Fact]
	public void Validate_DerivesOverallGpuStatusFromPerDeviceResults()
	{
		QualificationProfile profile = QualificationProfiles.Quick;
		WorkloadValidator validator = new();
		WorkloadStatus workload = new()
		{
			State = WorkloadState.Running,
			GpuEnabled = true,
			SelectedGpuIdentifiers = ["GPU-A", "GPU-B"]
		};
		SystemSnapshot telemetry = new()
		{
			GpuTelemetrySnapshots =
			[
				CreateGpu("GPU-A", 100, 50),
				CreateGpu("GPU-B", 100, 100)
			]
		};

		validator.Validate(workload, telemetry, profile, TimeSpan.Zero);
		TimeSpan afterWarmup = profile.GpuWarmupDuration + TimeSpan.FromSeconds(1);
		validator.Validate(workload, telemetry, profile, afterWarmup);
		ValidationResult result = validator.Validate(
			workload,
			telemetry,
			profile,
			afterWarmup + profile.GpuStabilityDuration + TimeSpan.FromSeconds(1));

		Assert.Equal(2, result.GpuDevices.Count);
		Assert.Equal(ValidationStatus.Fail, result.GpuStatus);
		Assert.Equal(ValidationStatus.Pass, result.GpuDevices[0].Status);
		Assert.Equal(ValidationStatus.Fail, result.GpuDevices[1].Status);
	}

	[Fact]
	public void Validate_MapsTerminalGpuWorkloadFailureToTheOwningDevice()
	{
		WorkloadValidator validator = new();
		WorkloadStatus workload = new()
		{
			State = WorkloadState.Failed,
			GpuEnabled = true,
			SelectedGpuIdentifiers = ["GPU-A", "GPU-B"],
			GpuDevices =
			[
				new GpuWorkloadDeviceStatus(
					"GPU-A",
					WorkloadState.Stopped,
					"Stopped after another GPU workload failed"),
				new GpuWorkloadDeviceStatus(
					"GPU-B",
					WorkloadState.Failed,
					"GPU GPU-B failed: Direct3D 12 GPU workload timed out.")
			]
		};
		SystemSnapshot telemetry = new()
		{
			GpuTelemetrySnapshots =
			[
				CreateGpu("GPU-A", 100, 50),
				CreateGpu("GPU-B", 100, 50)
			]
		};

		ValidationResult result = validator.Validate(
			workload,
			telemetry,
			QualificationProfiles.Quick,
			TimeSpan.FromSeconds(1));

		Assert.Equal(ValidationStatus.Fail, result.GpuStatus);
		Assert.Equal(ValidationStatus.Unknown, result.GpuDevices[0].Status);
		Assert.Equal(ValidationStatus.Fail, result.GpuDevices[1].Status);
		Assert.Contains("another GPU workload failed", result.GpuDevices[0].Message);
		Assert.Contains("timed out", result.GpuDevices[1].Message);
	}

	[Fact]
	public void Validate_PreservesUnavailableTelemetryForOneSelectedDevice()
	{
		QualificationProfile profile = QualificationProfiles.Quick;
		WorkloadValidator validator = new();
		WorkloadStatus workload = new()
		{
			State = WorkloadState.Running,
			GpuEnabled = true,
			SelectedGpuIdentifiers = ["GPU-A", "GPU-B"]
		};
		SystemSnapshot telemetry = new()
		{
			GpuTelemetrySnapshots =
			[
				CreateGpu("GPU-A", 100, 50)
			]
		};

		validator.Validate(workload, telemetry, profile, TimeSpan.Zero);
		TimeSpan afterWarmup = profile.GpuWarmupDuration + TimeSpan.FromSeconds(1);
		validator.Validate(workload, telemetry, profile, afterWarmup);
		ValidationResult result = validator.Validate(
			workload,
			telemetry,
			profile,
			afterWarmup + profile.GpuStabilityDuration + TimeSpan.FromSeconds(1));

		Assert.Equal(ValidationStatus.Warning, result.GpuStatus);
		Assert.True(result.GpuDevices[0].TelemetryAvailable);
		Assert.False(result.GpuDevices[1].TelemetryAvailable);
		Assert.Equal(ValidationStatus.Unknown, result.GpuDevices[1].Status);
	}

	private static GpuTelemetrySnapshot CreateGpu(
		string identifier,
		double load,
		int temperature)
	{
		return new GpuTelemetrySnapshot
		{
			Identifier = identifier,
			Name = identifier,
			IsAvailable = true,
			LoadPercent = load,
			TemperatureCelsius = temperature,
			MemoryTotalBytes = 100,
			MemoryUsedBytes = 50,
			Status = "OK"
		};
	}
}
