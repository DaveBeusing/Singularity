// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Monitoring.Models;

namespace Singularity.Tests.Validation;

public sealed class WorkloadValidatorTests
{
	[Fact]
	public void Validate_UsesProfileThresholdsForCpuAndMemory()
	{
		WorkloadStatus workload = new()
		{
			State = WorkloadState.Running,
			CpuEnabled = true,
			MemoryEnabled = true,
			MemoryGb = 1,
			MemoryAllocatedMb = 800
		};
		SystemSnapshot telemetry = new() { CpuLoadPercent = 76 };

		ValidationResult result = new WorkloadValidator().Validate(
			workload, telemetry, QualificationProfiles.Quick, TimeSpan.FromSeconds(30));

		Assert.Equal(ValidationStatus.Pass, result.CpuStatus);
		Assert.Equal(ValidationStatus.Warning, result.MemoryStatus);
		Assert.Equal(ValidationStatus.Unknown, result.GpuStatus);
	}

	[Fact]
	public void Validate_DoesNotFailGpuDuringWarmup()
	{
		WorkloadValidator validator = new();
		WorkloadStatus workload = new() { State = WorkloadState.Running, GpuEnabled = true };
		SystemSnapshot telemetry = new()
		{
			GpuTelemetryAvailable = true,
			GpuLoadPercent = 0,
			GpuTemperatureCelsius = 40
		};

		ValidationResult result = validator.Validate(
			workload, telemetry, QualificationProfiles.Quick, TimeSpan.FromSeconds(1));

		Assert.Equal(ValidationStatus.Warning, result.GpuStatus);
		Assert.Equal("GPU warming up", result.GpuMessage);
	}
	[Fact]
	public void Validate_UsesTelemetryForExplicitlySelectedGpuRegardlessOfOrder()
	{
		QualificationProfile profile = QualificationProfiles.Quick;
		WorkloadValidator validator = new();
		WorkloadStatus workload = new()
		{
			State = WorkloadState.Running,
			GpuEnabled = true,
			SelectedGpuIdentifier = "GPU-B"
		};
		SystemSnapshot telemetry = new()
		{
			GpuTelemetryAvailable = true,
			GpuLoadPercent = 100,
			GpuTemperatureCelsius = 35,
			GpuTelemetrySnapshots =
			[
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-A",
					IsAvailable = true,
					LoadPercent = 100,
					TemperatureCelsius = 35,
					Status = "OK"
				},
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-B",
					IsAvailable = true,
					LoadPercent = 100,
					TemperatureCelsius = Convert.ToInt32(profile.GpuMaximumTemperatureCelsius + 10),
					Status = "OK"
				}
			]
		};

		validator.Validate(workload, telemetry, profile, TimeSpan.Zero);
		ValidationResult result = validator.Validate(
			workload,
			telemetry,
			profile,
			profile.GpuWarmupDuration + TimeSpan.FromSeconds(1));

		Assert.Equal(ValidationStatus.Fail, result.GpuStatus);
		Assert.Contains("temperature", result.GpuMessage, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Validate_DoesNotFallBackToAnotherGpuWhenSelectedTelemetryIsUnavailable()
	{
		QualificationProfile profile = QualificationProfiles.Quick;
		WorkloadValidator validator = new();
		WorkloadStatus workload = new()
		{
			State = WorkloadState.Running,
			GpuEnabled = true,
			SelectedGpuIdentifier = "GPU-B"
		};
		SystemSnapshot telemetry = new()
		{
			GpuTelemetryAvailable = true,
			GpuLoadPercent = 100,
			GpuTemperatureCelsius = 35,
			GpuTelemetrySnapshots =
			[
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-A",
					IsAvailable = true,
					LoadPercent = 100,
					TemperatureCelsius = 35,
					Status = "OK"
				},
				new GpuTelemetrySnapshot
				{
					Identifier = "GPU-B",
					IsAvailable = false,
					Status = "Selected GPU sensor unavailable"
				}
			]
		};

		validator.Validate(workload, telemetry, profile, TimeSpan.Zero);
		ValidationResult result = validator.Validate(
			workload,
			telemetry,
			profile,
			profile.GpuWarmupDuration + TimeSpan.FromSeconds(1));

		Assert.Equal(ValidationStatus.Warning, result.GpuStatus);
		Assert.Equal("Selected GPU sensor unavailable", result.GpuMessage);
	}
}
