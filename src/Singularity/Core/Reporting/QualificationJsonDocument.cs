// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.Core.Reporting;

public sealed record QualificationJsonDocument(
	string SchemaVersion,
	string SingularityVersion,
	DateTime Timestamp,
	TimeSpan SessionDuration,
	QualificationProfile QualificationProfile,
	QualificationValidationJson Validation,
	SessionTelemetryStatistics TelemetryStatistics,
	IReadOnlyList<GpuQualificationEvidenceJson> GpuEvidence,
	HardwareSummaryJson Hardware);

public sealed record QualificationValidationJson(
	ValidationStatus Cpu,
	ValidationStatus Memory,
	ValidationStatus Gpu,
	ValidationStatus Overall);

public sealed record GpuQualificationEvidenceJson(
	string Identifier,
	string Name,
	ValidationStatus Validation,
	string ValidationMessage,
	bool TelemetryAvailable,
	GpuTelemetryStatisticsJson Telemetry);

public sealed record GpuTelemetryStatisticsJson(
	long AvailableSampleCount,
	long UnavailableSampleCount,
	MetricStatistics? LoadPercent,
	MetricStatistics? TemperatureCelsius,
	MetricStatistics? PowerWatts,
	MetricStatistics? VramUsagePercent);

public sealed record HardwareSummaryJson(
	string OperatingSystem,
	string ComputerName,
	string Mainboard,
	string Processor,
	int ProcessorCores,
	int ProcessorThreads,
	IReadOnlyList<GpuSummaryJson> Gpus,
	IReadOnlyList<string> MemoryModules,
	IReadOnlyList<string> StorageDrives);

public sealed record GpuSummaryJson(
	string Identifier,
	string Name,
	string Vram,
	string PcieLink);
