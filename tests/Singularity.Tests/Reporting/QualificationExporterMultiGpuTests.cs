// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.Hardware.Models;

namespace Singularity.Tests.Reporting;

public sealed class QualificationExporterMultiGpuTests
{
	[Fact]
	public void JsonExporter_WritesVersionedStablePerGpuEvidence()
	{
		QualificationReport report = CreateReport();
		HardwareInventory hardware = CreateHardware();

		string json = new QualificationJsonExporter().Serialize(
			report,
			hardware,
			"1.2.3");

		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement root = document.RootElement;
		Assert.Equal("4.0", root.GetProperty("schemaVersion").GetString());
		Assert.Equal("Fail", root.GetProperty("validation").GetProperty("gpu").GetString());

		JsonElement evidence = root.GetProperty("gpuEvidence");
		Assert.Equal(2, evidence.GetArrayLength());
		Assert.Equal("GPU-A", evidence[0].GetProperty("identifier").GetString());
		Assert.Equal("Pass", evidence[0].GetProperty("validation").GetString());
		Assert.Equal(2, evidence[0].GetProperty("telemetry").GetProperty("availableSampleCount").GetInt64());
		Assert.Equal("GPU-B", evidence[1].GetProperty("identifier").GetString());
		Assert.Equal("Fail", evidence[1].GetProperty("validation").GetString());
		Assert.Equal(1, evidence[1].GetProperty("telemetry").GetProperty("unavailableSampleCount").GetInt64());
		JsonElement timeline = root.GetProperty("telemetryTimeline");
		Assert.Equal(2, timeline.GetProperty("points").GetArrayLength());
		Assert.Equal("GPU-A", timeline.GetProperty("points")[0].GetProperty("gpus")[0].GetProperty("identifier").GetString());
	}

	[Fact]
	public void HtmlExporter_RendersOneEvidenceSectionPerSelectedGpu()
	{
		string html = new QualificationHtmlExporter().Render(
			CreateReport(),
			CreateHardware(),
			"1.2.3");

		Assert.Contains("GPU DEVICE EVIDENCE", html, StringComparison.Ordinal);
		Assert.Contains("GPU-A", html, StringComparison.Ordinal);
		Assert.Contains("GPU-B", html, StringComparison.Ordinal);
		Assert.Contains("GPU A", html, StringComparison.Ordinal);
		Assert.Contains("GPU B", html, StringComparison.Ordinal);
		Assert.Contains("Schema 4.0", html, StringComparison.Ordinal);
		Assert.Contains("Thermal limit exceeded", html, StringComparison.Ordinal);
		Assert.Contains("TELEMETRY TIMELINE", html, StringComparison.Ordinal);
		Assert.Contains("GPU temperature", html, StringComparison.Ordinal);
	}

	[Fact]
	public void JsonExporter_KeepsSingleGpuCompatibilityFields()
	{
		QualificationReport baseline = CreateReport();
		QualificationReport report = new()
		{
			StartedAt = baseline.StartedAt,
			FinishedAt = baseline.FinishedAt,
			Duration = baseline.Duration,
			Profile = baseline.Profile,
			GpuResult = ValidationStatus.Pass,
			OverallResult = ValidationStatus.Pass,
			TelemetryTimeline = CreateTimeline(),
			TelemetryStatistics = new SessionTelemetryStatistics
			{
				GpuLoadPercent = Metric(1, 90, 90, 90),
				GpuTemperatureCelsius = Metric(1, 55, 55, 55),
				Gpus = [CreateEvidence("GPU-A", "GPU A", ValidationStatus.Pass, "GPU load 90%", 1, 0).TelemetryStatistics]
			},
			GpuEvidence =
			[
				CreateEvidence("GPU-A", "GPU A", ValidationStatus.Pass, "GPU load 90%", 1, 0)
			]
		};

		string json = new QualificationJsonExporter().Serialize(
			report,
			CreateHardware(),
			"1.2.3");

		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement root = document.RootElement;
		Assert.Equal(
			"Pass",
			root.GetProperty("validation").GetProperty("gpu").GetString());
		Assert.Equal(
			90,
			root.GetProperty("telemetryStatistics")
				.GetProperty("gpuLoadPercent")
				.GetProperty("average")
				.GetDouble());
		Assert.Single(root.GetProperty("gpuEvidence").EnumerateArray());
	}

	private static QualificationReport CreateReport()
	{
		return new QualificationReport
		{
			StartedAt = new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc),
			FinishedAt = new DateTime(2026, 9, 23, 10, 5, 0, DateTimeKind.Utc),
			Duration = TimeSpan.FromMinutes(5),
			Profile = QualificationProfiles.Quick,
			CpuResult = ValidationStatus.Pass,
			MemoryResult = ValidationStatus.Pass,
			GpuResult = ValidationStatus.Fail,
			OverallResult = ValidationStatus.Fail,
			TelemetryTimeline = CreateTimeline(),
			TelemetryStatistics = new SessionTelemetryStatistics
			{
				CpuLoadPercent = Metric(2, 80, 90, 100),
				SystemMemoryUsagePercent = Metric(2, 40, 50, 60),
				Gpus =
				[
					CreateEvidence("GPU-A", "GPU A", ValidationStatus.Pass, "GPU load stable", 2, 0).TelemetryStatistics,
					CreateEvidence("GPU-B", "GPU B", ValidationStatus.Fail, "Thermal limit exceeded", 1, 1).TelemetryStatistics
				]
			},
			GpuEvidence =
			[
				CreateEvidence("GPU-A", "GPU A", ValidationStatus.Pass, "GPU load stable", 2, 0),
				CreateEvidence("GPU-B", "GPU B", ValidationStatus.Fail, "Thermal limit exceeded", 1, 1)
			]
		};
	}

	private static GpuQualificationEvidence CreateEvidence(
		string identifier,
		string name,
		ValidationStatus result,
		string message,
		long availableSamples,
		long unavailableSamples)
	{
		return new GpuQualificationEvidence
		{
			Identifier = identifier,
			Name = name,
			Result = result,
			ValidationMessage = message,
			TelemetryAvailable = availableSamples > 0,
			TelemetryStatistics = new GpuTelemetryStatistics
			{
				Identifier = identifier,
				Name = name,
				AvailableSampleCount = availableSamples,
				UnavailableSampleCount = unavailableSamples,
				LoadPercent = Metric(availableSamples, 70, 85, 100),
				TemperatureCelsius = Metric(availableSamples, 50, 60, 70),
				PowerWatts = Metric(availableSamples, 100, 120, 140),
				VramUsagePercent = Metric(availableSamples, 20, 40, 60)
			}
		};
	}

	private static QualificationTelemetryTimeline CreateTimeline()
	{
		return new QualificationTelemetryTimeline
		{
			SamplingInterval = TimeSpan.FromSeconds(1),
			Points =
			[
				new QualificationTelemetryPoint
				{
					Elapsed = TimeSpan.Zero,
					CpuLoadPercent = 80,
					SystemMemoryUsagePercent = 40,
					Gpus =
					[
						new QualificationTelemetryGpuPoint
						{
							Identifier = "GPU-A",
							Name = "GPU A",
							LoadPercent = 70,
							TemperatureCelsius = 50,
							PowerWatts = 100,
							VramUsagePercent = 20
						},
						new QualificationTelemetryGpuPoint
						{
							Identifier = "GPU-B",
							Name = "GPU B",
							LoadPercent = 75,
							TemperatureCelsius = 55,
							PowerWatts = 110,
							VramUsagePercent = 30
						}
					]
				},
				new QualificationTelemetryPoint
				{
					Elapsed = TimeSpan.FromSeconds(1),
					CpuLoadPercent = 90,
					SystemMemoryUsagePercent = 50,
					Gpus =
					[
						new QualificationTelemetryGpuPoint
						{
							Identifier = "GPU-A",
							Name = "GPU A",
							LoadPercent = 90,
							TemperatureCelsius = 60,
							PowerWatts = 120,
							VramUsagePercent = 40
						},
						new QualificationTelemetryGpuPoint
						{
							Identifier = "GPU-B",
							Name = "GPU B"
						}
					]
				}
			],
			Events =
			[
				new QualificationTimelineEvent
				{
					Elapsed = TimeSpan.Zero,
					Kind = QualificationTimelineEventKind.Start,
					Label = "Qualification started"
				}
			]
		};
	}

	private static MetricStatistics? Metric(
		long sampleCount,
		double minimum,
		double average,
		double maximum)
	{
		return sampleCount == 0
			? null
			: new MetricStatistics
			{
				SampleCount = sampleCount,
				Minimum = minimum,
				Average = average,
				Maximum = maximum
			};
	}

	private static HardwareInventory CreateHardware()
	{
		return new HardwareInventory
		{
			Gpus =
			[
				new GpuInventory { Identifier = "GPU-A", Name = "GPU A" },
				new GpuInventory { Identifier = "GPU-B", Name = "GPU B" }
			]
		};
	}
}
