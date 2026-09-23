// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.Monitoring.Models;

namespace Singularity.Tests.Reporting;

public sealed class MultiGpuQualificationEvidenceTests
{
	[Fact]
	public void SessionAndReport_FreezePerGpuStatisticsAndValidation()
	{
		QualificationSession session = new();
		session.Start(
			QualificationProfiles.Quick,
			selectedGpuIdentifiers: ["GPU-A", "GPU-B"]);
		session.RecordTelemetry(new SystemSnapshot
		{
			GpuTelemetrySnapshots =
			[
				CreateGpu("GPU-A", "GPU A", 80, 50),
				CreateGpu("GPU-B", "GPU B", 40, 60)
			]
		});
		session.RecordTelemetry(new SystemSnapshot
		{
			GpuTelemetrySnapshots =
			[
				CreateGpu("GPU-A", "GPU A", 100, 54)
			]
		});
		session.Complete(ValidationStatus.Warning);

		ValidationResult validation = new()
		{
			GpuStatus = ValidationStatus.Warning,
			GpuDevices =
			[
				new GpuValidationResult("GPU-A", "GPU A", ValidationStatus.Pass, "GPU load 100%", true),
				new GpuValidationResult("GPU-B", "GPU B", ValidationStatus.Warning, "Telemetry unavailable", false)
			]
		};

		QualificationReport report =
			new QualificationReportGenerator().Create(session, validation);

		Assert.Equal(2, session.TelemetryStatistics.Gpus.Count);
		Assert.Null(session.TelemetryStatistics.GpuLoadPercent);
		Assert.Equal(90, report.GpuEvidence[0].TelemetryStatistics.LoadPercent!.Average);
		Assert.Equal(1, report.GpuEvidence[1].TelemetryStatistics.AvailableSampleCount);
		Assert.Equal(1, report.GpuEvidence[1].TelemetryStatistics.UnavailableSampleCount);
		Assert.Equal(ValidationStatus.Warning, report.GpuEvidence[1].Result);
		Assert.False(report.GpuEvidence[1].TelemetryAvailable);
	}

	[Fact]
	public void History_PreservesGpuEvidenceWhenReportIsUnavailable()
	{
		QualificationSession session = new();
		session.Start(
			QualificationProfiles.Quick,
			selectedGpuIdentifiers: ["GPU-A"]);
		session.RecordTelemetry(new SystemSnapshot
		{
			GpuTelemetrySnapshots =
			[
				CreateGpu("GPU-A", "GPU A", 90, 55)
			]
		});
		session.Fail();

		QualificationHistory history = new();
		history.Add(session);

		GpuQualificationEvidence evidence =
			Assert.Single(Assert.Single(history.Records).GpuEvidence);
		Assert.Equal("GPU-A", evidence.Identifier);
		Assert.Equal(ValidationStatus.Unknown, evidence.Result);
		Assert.True(evidence.TelemetryAvailable);
	}

	private static GpuTelemetrySnapshot CreateGpu(
		string identifier,
		string name,
		double load,
		int temperature)
	{
		return new GpuTelemetrySnapshot
		{
			Identifier = identifier,
			Name = name,
			IsAvailable = true,
			LoadPercent = load,
			TemperatureCelsius = temperature,
			MemoryTotalBytes = 100,
			MemoryUsedBytes = 50,
			Status = "OK"
		};
	}
}
